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
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<SignInService> _logger;

    public SignInService(
        ICompanyRepository companyRepository,
        IAccessTokenService accessTokenService,
        IPasswordHasher<ApplicationUser> passwordHasher, 
        ILogger<SignInService> logger)
    {
        _companyRepository = companyRepository;
        _accessTokenService = accessTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<SignInResponse> SignInAsync(SignInDto signInDto)
    {
        try
        {
            // Try to find the company (CompanyEntity) by email from the SignInDto
            var companyEntity = await _companyRepository.GetByEmailAsync(signInDto.Email);
            if (companyEntity == null)
            {
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Företaget finns inte."
                };
            }

            // Create an ApplicationUser from the CompanyEntity (mapping relevant fields)
            var applicationUser = new ApplicationUser
            {
                Id = companyEntity.Id.ToString(),
                UserName = companyEntity.Email,
                Email = companyEntity.Email,
                PasswordHash = companyEntity.PasswordHash,
                CompanyName = companyEntity.CompanyName,
                OrganisationNumber = companyEntity.OrganizationNumber,
                IsVerified = companyEntity.IsVerified
            };

            // Validate the password by comparing the hashed version using PasswordHasher
            var passwordValid = _passwordHasher.VerifyHashedPassword(applicationUser, applicationUser.PasswordHash, signInDto.Password);
            if (passwordValid != PasswordVerificationResult.Success)
            {
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Felaktiga inloggningsuppgifter."
                };
            }

            // If company is verified and password is correct, generate the token
            var token = _accessTokenService.GenerateAccessToken(applicationUser);

            return new SignInResponse
            {
                Success = true,
                Token = token,
                Message = "Inloggning lyckades.",
                User = applicationUser
            };
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An error occurred while signing in.");

            // Return a response indicating failure
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ett fel inträffade vid inloggning. Försök igen senare."
            };
        }
    }
}
