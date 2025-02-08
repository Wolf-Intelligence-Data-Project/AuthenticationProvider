using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthenticationProvider.Services.Security;

public class AccountVerificationService : IAccountVerificationService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationClient _accountVerificationClient;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<AccountVerificationService> _logger;
    private readonly IConfiguration _configuration;

    public AccountVerificationService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationClient accountVerificationClient,
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationService> logger,
        IConfiguration configuration)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _accountVerificationClient = accountVerificationClient;
        _accountVerificationTokenService = accountVerificationTokenService;
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
            if (!await EmailValidation(token))
            {
                _logger.LogWarning("The email in the token does not match any company.");
                return ServiceResult.EmailNotFound;
            }

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

        // Extra check of the token before starting the process (where it checks again)
        var accountVerificationToken = await _accountVerificationTokenService.GetValidAccountVerificationTokenAsync(token);
        if (accountVerificationToken == null)
        {
            _logger.LogWarning("The verification token is invalid.");
            return ServiceResult.InvalidToken;  // Return failure if token is invalid
        }

        // Extract companyId from the accountVerificationToken
        var companyId = accountVerificationToken.CompanyId;

        var company = await _companyRepository.GetByIdAsync(companyId);  // Fetch the company using companyId
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

        // Mark the account verification token as used after the company is successfully verified
        await _accountVerificationTokenService.MarkAccountVerificationTokenAsUsedAsync(token);

        _logger.LogInformation("Account verified successfully.");
        return ServiceResult.Success;  // Return success once everything is verified
    }

    public async Task<ServiceResult> ResendVerificationEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("No email provided for resending verification email.");
            return ServiceResult.Failure;
        }

        try
        {
            // Fetch the company by email
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return ServiceResult.Failure;  // Use Failure property directly
            }

            // Set the company as not verified
            company.IsVerified = false;
            await _companyRepository.UpdateAsync(company);

            // Generate a new account verification token for the company
            var newToken = await _accountVerificationTokenService.CreateAccountVerificationTokenAsync(company.Id);
            if (string.IsNullOrEmpty(newToken))
            {
                _logger.LogError("Failed to create a new verification token for company with email: {Email}", email);
                return ServiceResult.Failure;  // Use Failure property directly
            }

            // Send the new verification token to the email verification provider
            var emailSent = await SendVerificationEmailAsync(newToken);
            if (emailSent != ServiceResult.Success)
            {
                return ServiceResult.Failure;  // Use Failure property directly
            }

            _logger.LogInformation("Verification email resent to company with email: {Email}", email);

            return ServiceResult.Success;  // Return Success if everything went well
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending the verification email for email: {Email}", email);
            return ServiceResult.Failure;  // Use Failure property directly
        }
    }

    private async Task<bool> EmailValidation(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Log all claims for debugging (optional)
            foreach (var claim in jwtToken.Claims)
            {
                _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            }

            // Extract the email claim from the token
            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(emailClaim))
            {
                _logger.LogWarning("No email claim found in the token.");
                return false;
            }

            // Check if a company exists with the extracted email
            var company = await _companyRepository.GetByEmailAsync(emailClaim);
            if (company == null)
            {
                _logger.LogWarning("No company found with the email from the token.");
                return false;
            }

            // Check if the company is already verified (optional)
            if (company.IsVerified)
            {
                _logger.LogInformation("The company is already verified.");
                return false; // If already verified, return false or handle as needed
            }

            return true; // Email claim exists, matches a company, and company is not verified yet
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating email from token.");
            return false;
        }
    }
}
