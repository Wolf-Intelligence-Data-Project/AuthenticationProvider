using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Services.Tokens;

public class ResetPasswordTokenService : IResetPasswordTokenService
{
    private readonly IResetPasswordTokenRepository _resetPasswordTokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResetPasswordTokenService> _logger;
    private readonly PasswordHasher<CompanyEntity> _passwordHasher;

    public ResetPasswordTokenService(
        IResetPasswordTokenRepository resetPasswordTokenRepository,
        ICompanyRepository companyRepository,
        IConfiguration configuration,
        ILogger<ResetPasswordTokenService> logger)
    {
        _resetPasswordTokenRepository = resetPasswordTokenRepository;
        _companyRepository = companyRepository;
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = new PasswordHasher<CompanyEntity>(); 
    }

    // Create a reset password token for a company identified by email.
    public async Task<string> CreateResetPasswordTokenAsync(string email)
    {
        try
        {
            // Check if email is valid
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email is null or empty.");
                throw new ArgumentException("E-postadress är obligatorisk.");
            }

            // Fetch the company by email
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                throw new ArgumentException("Inget företag hittades med den angivna e-postadressen.");
            }

            // Delete any existing reset password tokens for the company
            await _resetPasswordTokenRepository.DeleteAsync(company.Id);

            // JWT configuration and validation
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentNullException("JWT-inställningar saknas i konfigurationen.");
            }

            // Setup security and signing credentials for the JWT
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Define the token's claims and expiration
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                {
                    new Claim(ClaimTypes.NameIdentifier, company.Id.ToString()),
                    new Claim(ClaimTypes.Email, company.Email),
                    new Claim("token_type", "ResetPassword"),
                    new Claim(JwtRegisteredClaimNames.Aud, audience)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            // Create the JWT token
            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            // Create the reset password token entity
            var resetPasswordToken = new ResetPasswordTokenEntity
            {
                Token = tokenString,
                CompanyId = company.Id,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            // Save the reset password token to the repository
            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);

            return tokenString;
        }
        catch (ArgumentException ex)
        {
            // Handle argument exceptions
            _logger.LogWarning(ex, "Validation error.");
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected errors
            _logger.LogError(ex, "An unexpected error occurred.");
            throw new InvalidOperationException("Ett oväntat fel uppstod, försök igen senare.");
        }
    }

    // Method to get a valid reset password token from the repository
    public async Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !(await ValidateResetPasswordTokenAsync(token)))
        {
            _logger.LogWarning("Invalid or expired reset password token provided.");
            throw new ArgumentException("Tokenet är ogiltigt eller har löpt ut.");
        }

        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("The reset password token is invalid, expired, or already used.");
                return null;
            }

            return resetPasswordToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating reset password token.");
            throw new InvalidOperationException("Ett fel uppstod vid validering av tokenet. Försök igen senare.");
        }
    }

    // Extract the email from the reset password token
    public async Task<string> GetEmailFromTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !(await ValidateResetPasswordTokenAsync(token)))
        {
            _logger.LogWarning("Invalid or expired reset password token provided.");
            return null;
        }

        try
        {
            // Retrieve the reset password token from the repository
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("The reset password token is invalid or already used.");
                return null;
            }

            // Retrieve the associated company by the token's company ID
            var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
            return company?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting email from reset password token.");
            throw new InvalidOperationException("Ett fel uppstod vid hämtning av e-post. Försök igen senare.");
        }
    }

    // Mark the reset password token as used after successful password reset
    public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            // Retrieve the reset password token by its ID
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !(await ValidateResetPasswordTokenAsync(resetPasswordToken.Token)))
            {
                _logger.LogWarning("The reset password token is invalid or does not exist.");
                return;
            }

            // Mark the token as used and update its status
            resetPasswordToken.IsUsed = true;
            await _resetPasswordTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ett fel uppstod vid markering av token som använd.");
        }
    }

    // Delete all reset password tokens for a company
    public async Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId)
    {
        try
        {
            await _resetPasswordTokenRepository.DeleteAsync(companyId);
            _logger.LogInformation("All reset password tokens for company {CompanyId} have been deleted.", companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting reset password tokens for company {CompanyId}.", companyId);
            throw new InvalidOperationException("Ett fel uppstod vid radering av token. Försök igen senare.");
        }
    }

    // Helper method to validate the reset password token
    public async Task<bool> ValidateResetPasswordTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token is null or empty.");
            return false;
        }

        try
        {
            // Parse the token and validate it against stored records
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            return resetPasswordToken != null && !resetPasswordToken.IsUsed && resetPasswordToken.ExpiryDate >= DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating the reset password token.");
            return false;
        }
    }
}
