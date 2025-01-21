using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationProvider.Interfaces.Services;

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


    // Creates a reset password token for a company based on email
    public async Task<string> CreateResetPasswordTokenAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Email is null or empty.");
            throw new ArgumentException("Email is required to generate reset password token.");
        }

        // Fetch the company by email
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            _logger.LogWarning("Company not found with email: {Email}", email);
            throw new ArgumentException("No company found with the provided email.");
        }

        // Delete existing tokens for the company
        await _resetPasswordTokenRepository.DeleteAsync(company.Id);

        // Generate a new reset password token
        var secretKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
        {
            throw new ArgumentNullException("JWT settings are missing in the configuration.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

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
            Audience = audience, // Add audience to the descriptor
            SigningCredentials = credentials
        };

        var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(jwtToken);

        // Save the token in the database
        var resetPasswordToken = new ResetPasswordTokenEntity
        {
            Token = tokenString,
            CompanyId = company.Id,
            ExpiryDate = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);
        _logger.LogInformation("Reset password token created for company: {CompanyId}", company.Id);

        return tokenString;
    }




    // Retrieves a valid reset password token, ensuring it's not expired or already used
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

    // Extracts the email address from a reset password token
    public async Task<string> GetEmailFromTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !(await ValidateResetPasswordTokenAsync(token)))
        {
            _logger.LogWarning("Invalid or expired reset password token provided.");
            return null;
        }

        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("The reset password token is invalid or already used.");
                return null;
            }

            var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
            return company?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting email from reset password token.");
            throw new InvalidOperationException("Ett fel uppstod vid hämtning av e-post. Försök igen senare.");
        }
    }

    // Resets the company password by updating it in the database
    public async Task<bool> ResetCompanyPasswordAsync(string email, string newPassword)
    {
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            _logger.LogWarning("Company not found with email: {Email}", email);
            return false;
        }

        try
        {
            var hashedPassword = _passwordHasher.HashPassword(null, newPassword);
            company.PasswordHash = hashedPassword;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Password reset successfully for company: {CompanyId}", company.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while resetting password for company: {CompanyId}", company.Id);
            throw new InvalidOperationException("Ett fel uppstod vid återställning av lösenord. Försök igen senare.");
        }
    }

    // Marks a reset password token as used after successful password reset
    public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !(await ValidateResetPasswordTokenAsync(resetPasswordToken.Token)))
            {
                _logger.LogWarning("The reset password token is invalid or does not exist: {TokenId}", tokenId);
                return;
            }

            resetPasswordToken.IsUsed = true;
            await _resetPasswordTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
            _logger.LogInformation("Reset password token marked as used: {TokenId}", tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking reset password token as used: {TokenId}", tokenId);
            throw new InvalidOperationException("Ett fel uppstod vid uppdatering av tokenstatus. Försök igen senare.");
        }
    }

    // Deletes all reset password tokens associated with a company
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

    // Validates a reset password token by checking its signature and expiration
    public async Task<bool> ValidateResetPasswordTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is null or empty.");
            return false;
        }

        try
        {
            // Retrieve the token from the database
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Reset password token is either invalid, already used, or expired.");
                return false;
            }

            // Token is valid if it exists and is not expired or used
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating the reset password token.");
            throw new InvalidOperationException("Ett fel uppstod vid validering av tokenet. Försök igen senare.");
        }
    }

}
