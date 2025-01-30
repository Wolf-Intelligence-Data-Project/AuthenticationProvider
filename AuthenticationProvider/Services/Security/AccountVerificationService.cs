using AuthenticationProvider.Interfaces.Clients;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Security;
using AuthenticationProvider.Models.Responses;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services.Security;

public class AccountVerificationService : IAccountVerificationService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly IAccountVerificationClient _accountVerificationClient;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<AccountVerificationService> _logger;
    private readonly IConfiguration _configuration;

    public AccountVerificationService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        IAccountVerificationClient accountVerificationClient,
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationService> logger,
        IConfiguration configuration)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _accountVerificationClient = accountVerificationClient;
        _companyRepository = companyRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ServiceResult> SendVerificationEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("Token is null or empty.");
            return ServiceResult.InvalidToken;  // Returning InvalidToken on failure
        }

        try
        {
            var result = await _accountVerificationClient.SendVerificationEmailAsync(token);
            if (!result)
            {
                _logger.LogError("Failed to send email.");
                return ServiceResult.InvalidToken;  // Handle failure properly
            }

            return ServiceResult.Success;  // Return success if email was sent
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email");
            return ServiceResult.InvalidToken;  // Return failure on exception
        }
    }

    public async Task<ServiceResult> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No token provided for account verification.");
            return ServiceResult.InvalidToken;  // Return failure if token is invalid
        }

        var claimsPrincipal = ValidateAccountVerificationToken(token);
        if (claimsPrincipal == null)
        {
            return ServiceResult.InvalidToken;  // Token validation failed
        }

        var email = ExtractEmailFromToken(token);
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Email not found in token.");
            return ServiceResult.EmailNotFound;  // Return if email is not found
        }

        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            _logger.LogWarning("Company not found.");
            return ServiceResult.CompanyNotFound;  // Handle case when company is not found
        }

        if (company.IsVerified)
        {
            _logger.LogInformation("The company is already verified.");
            return ServiceResult.AlreadyVerified;  // Already verified, no further action needed
        }

        company.IsVerified = true;
        await _companyRepository.UpdateAsync(company);  // Update the company as verified

        if (!await RevokeTokenAsync(token))
        {
            return ServiceResult.InvalidToken;  // Handle revocation failure
        }

        _logger.LogInformation("Account verified successfully.");
        return ServiceResult.Success;  // Return success once everything is verified
    }

    private ClaimsPrincipal ValidateAccountVerificationToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero  // No clock skew for immediate expiration checks
            };

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return validatedToken is JwtSecurityToken ? claimsPrincipal : null;
        }
        catch (Exception ex) when (ex is SecurityTokenExpiredException || ex is SecurityTokenException)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return null;  // Return null if token is invalid or expired
        }
    }

    private string ExtractEmailFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var decodedToken = tokenHandler.ReadJwtToken(token);
            return decodedToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting email from token.");
            return null;  // Return null if extraction fails
        }
    }

    private async Task<bool> RevokeTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is null or empty.");
            return false;  // Return false if the token is invalid
        }

        try
        {
            await _accountVerificationTokenRepository.RevokeAndDeleteByTokenAsync(token);  // Revoke and delete the token
            return true;  // Return true if revocation was successful
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking the token.");
            return false;  // Return false if revocation failed
        }
    }
}
