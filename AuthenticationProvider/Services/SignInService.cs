using AuthenticationProvider.Models.Responses;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Dtos;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Services;

/// <summary>
/// Service responsible for handling sign-in logic for company authentication.
/// </summary>
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
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to sign in a company using email and password.
    /// Validates the provided credentials and generates an access token upon successful authentication.
    /// </summary>
    /// <param name="signInDto">The sign-in request containing email and password.</param>
    /// <returns>A response indicating success or failure, including an access token if successful.</returns>
    public async Task<SignInResponse> SignInAsync(SignInDto signInDto)
    {
        if (signInDto == null)
        {
            _logger.LogWarning("SignInDto is null.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ogiltig inloggningsförfrågan."
            };
        }

        if (string.IsNullOrWhiteSpace(signInDto.Email) || string.IsNullOrWhiteSpace(signInDto.Password))
        {
            _logger.LogWarning("Sign-in failed: Email or password is empty.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "E-post och lösenord krävs."
            };
        }

        try
        {
            // Retrieve the company by email
            var companyEntity = await _companyRepository.GetByEmailAsync(signInDto.Email);
            if (companyEntity == null)
            {
                _logger.LogWarning("Sign-in failed: Company not found for provided email.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Företaget finns inte."
                };
            }

            // Convert CompanyEntity to ApplicationUser for authentication
            var applicationUser = MapToApplicationUser(companyEntity);

            // Validate password
            if (!ValidatePassword(applicationUser, signInDto.Password))
            {
                _logger.LogWarning("Sign-in failed: Invalid credentials for the provided email.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Felaktiga inloggningsuppgifter."
                };
            }

            // Generate access token upon successful authentication
            var token = _accessTokenService.GenerateAccessToken(applicationUser);

            _logger.LogInformation("Company signed in successfully.");
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
            _logger.LogError(ex, "An error occurred during sign-in.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ett fel inträffade vid inloggning. Försök igen senare."
            };
        }
    }

    /// <summary>
    /// Maps a <see cref="CompanyEntity"/> to an <see cref="ApplicationUser"/> model.
    /// </summary>
    private ApplicationUser MapToApplicationUser(CompanyEntity companyEntity)
    {
        return new ApplicationUser
        {
            Id = companyEntity.Id.ToString(),
            UserName = companyEntity.Email,
            Email = companyEntity.Email,
            PasswordHash = companyEntity.PasswordHash,
            CompanyName = companyEntity.CompanyName,
            OrganisationNumber = companyEntity.OrganizationNumber,
            IsVerified = companyEntity.IsVerified
        };
    }

    /// <summary>
    /// Validates the provided password against the stored hash.
    /// </summary>
    /// <param name="user">The user whose password needs to be verified.</param>
    /// <param name="providedPassword">The password provided by the user.</param>
    /// <returns>True if the password is valid; otherwise, false.</returns>
    private bool ValidatePassword(ApplicationUser user, string providedPassword)
    {
        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
        return passwordResult == PasswordVerificationResult.Success;
    }
}
