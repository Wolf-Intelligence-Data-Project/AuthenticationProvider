using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging; // Add the logging namespace

namespace AuthenticationProvider.Services;

public class SendVerificationService : ISendVerificationService
{
    private readonly ISendVerificationClient _emailVerificationClient;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<SendVerificationService> _logger;

    public SendVerificationService(
        ISendVerificationClient emailVerificationClient,
        IAccountVerificationTokenService accountVerificationTokenService,
        ICompanyRepository companyRepository,
        ILogger<SendVerificationService> logger)
    {
        _emailVerificationClient = emailVerificationClient;
        _accountVerificationTokenService = accountVerificationTokenService;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<bool> SendVerificationEmailAsync(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("No company found for verification request.");
                return false;
            }

            if (company.IsVerified)
            {
                _logger.LogInformation("Attempted verification for already verified company.");
                return false;
            }

            if (!string.IsNullOrEmpty(company.LastEmailVerificationToken))
            {
                await _accountVerificationTokenService.RevokeVerificationTokenAsync(company.Id); // Revoke the existing token
                _logger.LogInformation("Revoked existing verification token for company (Token revoked).");
            }

            var newToken = await _accountVerificationTokenService.GenerateVerificationTokenAsync(company.Id);
            _logger.LogInformation("Generated new verification token (Token created)."); 

            var tokenSent = await _emailVerificationClient.SendVerificationEmailAsync(newToken);
            if (!tokenSent)
            {
                _logger.LogError("Failed to send verification email."); // General failure message
                return false;
            }

            await _companyRepository.UpdateEmailVerificationTokenAsync(company.Email, newToken);
            _logger.LogInformation("Verification email sent successfully (Email sent)."); // General success message

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing email verification request."); // General error message
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        try
        {
            var isExpired = await _accountVerificationTokenService.IsVerificationTokenExpiredAsync(token);
            if (isExpired)
            {
                _logger.LogWarning("Verification token is expired.");
                return false;  // Return false if token is expired
            }

            string email = ExtractEmailFromToken(token);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Invalid token: Unable to extract email.");
                return false;  // Return false if email is not valid
            }

            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found during email verification for the provided token.");
                return false;
            }

            if (company.LastEmailVerificationToken != token)
            {
                _logger.LogWarning("Verification token mismatch for provided token.");
                return false;  // Return false if token does not match
            }

            company.IsVerified = true;
            company.LastEmailVerificationToken = string.Empty;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Email verification successful (Email verified)."); // General success message

            return true;  // Successfully verified the email
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying the email.");
            return false;  // Return false if any error occurs
        }
    }

    private string ExtractEmailFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var decodedToken = tokenHandler.ReadJwtToken(token);
            var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == "sub");
            return emailClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting email from token.");
            throw new InvalidOperationException("Det uppstod ett problem med din begäran. Försök igen.", ex);
        }
    }
}
