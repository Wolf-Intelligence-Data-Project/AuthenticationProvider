using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Data.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Data;

namespace AuthenticationProvider.Services;

public class SignInService : ISignInService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;  // Dependency injection for PasswordHasher
    private readonly ILogger<SignInService> _logger;

    public SignInService(
        ICompanyRepository companyRepository,
        IAccessTokenService accessTokenService,
        IPasswordHasher<ApplicationUser> passwordHasher,  // Injecting PasswordHasher
        ILogger<SignInService> logger)
    {
        _companyRepository = companyRepository;
        _accessTokenService = accessTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<SignInResponse> SignInAsync(SignInDto signInDto)
    {
        // Try to find the company (CompanyEntity) by email from the SignInDto
        var companyEntity = await _companyRepository.GetByEmailAsync(signInDto.Email);
        if (companyEntity == null)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Företaget finns inte." // "The company does not exist."
            };
        }

        // Create an ApplicationUser from the CompanyEntity (mapping relevant fields)
        var applicationUser = new ApplicationUser
        {
            Id = companyEntity.Id.ToString(), // Convert Guid to string for IdentityUser
            UserName = companyEntity.Email, // Map the email to the username field
            Email = companyEntity.Email,
            PasswordHash = companyEntity.PasswordHash, 
            CompanyName = companyEntity.CompanyName, // Map additional fields
            OrganisationNumber = companyEntity.OrganisationNumber,
            IsVerified = companyEntity.IsVerified
        };

        // Validate the password by comparing the hashed version using PasswordHasher
        var passwordValid = _passwordHasher.VerifyHashedPassword(applicationUser, applicationUser.PasswordHash, signInDto.Password);
        if (passwordValid != PasswordVerificationResult.Success)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Felaktiga inloggningsuppgifter." // "Incorrect login credentials."
            };
        }

        // If company is verified and password is correct, generate the token
        var token = _accessTokenService.GenerateAccessToken(applicationUser); // Pass ApplicationUser

        return new SignInResponse
        {
            Success = true,
            Token = token,
            Message = "Inloggning lyckades.", // "Login successful."
            User = applicationUser // Return the original CompanyEntity as the user
        };
    }
}
