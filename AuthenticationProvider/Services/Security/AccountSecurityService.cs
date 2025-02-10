using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using AuthenticationProvider.Interfaces.Services.Tokens;

namespace AuthenticationProvider.Services.Security;

public class AccountSecurityService : IAccountSecurityService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly ILogger<AccountSecurityService> _logger;
    private readonly PasswordHasher<CompanyEntity> _passwordHasher;
    private readonly IEmailRestrictionService _emailRestrictionService;

    public AccountSecurityService(
        ICompanyRepository companyRepository,
        IAccessTokenService accessTokenService,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationService accountVerificationService,
        ILogger<AccountSecurityService> logger,
        IConfiguration configuration,
        IEmailRestrictionService emailRestrictionService)
    {
        _companyRepository = companyRepository;
        _accessTokenService = accessTokenService;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<CompanyEntity>();
        _emailRestrictionService = emailRestrictionService;
    }

    /// <summary>
    /// Changes the email address of a company after validating the token and email restrictions.
    /// </summary>
    /// <param name="emailChangeRequest">The request containing the new email and the token for authentication.</param>
    /// <returns>True if the email is successfully changed, false otherwise.</returns>
    public async Task<bool> ChangeEmailAsync(EmailChangeRequest emailChangeRequest)
    {
        try
        {
            // Validate email restrictions - prevent restricted email addresses
            if (_emailRestrictionService.IsRestrictedEmail(emailChangeRequest.Email))
            {
                _logger.LogWarning("Email change request denied for a restricted email.");
                return false; // Bad request: restricted email
            }

            var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(emailChangeRequest.Token); // Retrieve authentication and verification status

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired");
                return false;  // Token is invalid or expired
            }

            // Optionally, check if the token is verified if needed
            if (!isAccountVerified)
            {
                _logger.LogInformation("Account is not verified");
                return false; 
            }


            // Extract company ID from the token
            var companyIdString = _accessTokenService.GetUserIdFromToken(emailChangeRequest.Token);
            if (string.IsNullOrEmpty(companyIdString) || !Guid.TryParse(companyIdString, out Guid companyId))
            {
                _logger.LogWarning("Unable to retrieve or parse company ID from token.");
                return false; // Bad request: invalid company ID
            }

            // Retrieve the company entity by ID
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company not found for email change.");
                return false; // Not found: company does not exist
            }

            // Check if the new email is already in use
            var existingCompany = await _companyRepository.GetByEmailAsync(emailChangeRequest.Email);
            if (existingCompany != null)
            {
                _logger.LogWarning("Email is already in use.");
                return false; // Conflict: email already exists
            }

            // Set the company as unverified before changing the email
            company.IsVerified = false;
            await _companyRepository.UpdateAsync(company);

            // Update the company's email
            company.Email = emailChangeRequest.Email;
            await _companyRepository.UpdateAsync(company);

            // Generate a new verification token and send verification email
            var verificationToken = await _accountVerificationTokenService.GenerateAccountVerificationTokenAsync(companyId);
            var tokenResult = await _accountVerificationService.SendVerificationEmailAsync(verificationToken);

            if (tokenResult != ServiceResult.Success)
            {
                _logger.LogWarning("Failed to send account verification email.");
                return false; // Internal Server Error: failure in email sending
            }

            _logger.LogInformation("Email successfully changed and verification email sent.");
            return true; // Success: email changed and verification sent
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during email change: {ex.Message}");
            return false; // Internal Server Error: exception occurred
        }
    }

    /// <summary>
    /// Changes the password for a company after validating the token and password match.
    /// </summary>
    /// <param name="passwordChangeRequest">The request containing the new password, confirmation, and token for authentication.</param>
    /// <returns>True if the password is successfully changed, false otherwise.</returns>
    public async Task<bool> ChangePasswordAsync(PasswordChangeRequest passwordChangeRequest)
    {
        // Ensure valid request parameters
        if (string.IsNullOrWhiteSpace(passwordChangeRequest.Token) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.Password) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid password change request.");
            return false; // Bad request: missing password fields
        }

        // Ensure that the new password and confirm password match
        if (passwordChangeRequest.Password != passwordChangeRequest.ConfirmPassword)
        {
            _logger.LogWarning("New password and confirm password do not match.");
            return false; // Bad request: password mismatch
        }

        try
        {
            var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(passwordChangeRequest.Token); // Retrieve authentication and verification status

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired");
                return false;  // Token is invalid or expired
            }

            // Optionally, check if the token is verified if needed
            if (!isAccountVerified)
            {
                _logger.LogInformation("Token is not verified");
                return false;  // Token is not verified
            }


            // Extract company ID from the token
            var companyIdString = _accessTokenService.GetUserIdFromToken(passwordChangeRequest.Token);
            if (string.IsNullOrEmpty(companyIdString) || !Guid.TryParse(companyIdString, out Guid companyId))
            {
                _logger.LogWarning("Unable to retrieve or parse company ID from token.");
                return false; // Bad request: invalid company ID
            }

            // Retrieve the company entity by ID
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company not found for password change.");
                return false; // Not found: company does not exist
            }

            // Hash the new password and update the company's password hash
            company.PasswordHash = _passwordHasher.HashPassword(company, passwordChangeRequest.Password);
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Password successfully changed.");
            return true; // Success: password changed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing the password.");
            return false; // Internal Server Error: exception occurred
        }
    }
}
