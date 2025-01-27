using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationProvider.Services.Security;

public class AccountSecurityService : IAccountSecurityService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly ILogger<AccountSecurityService> _logger;
    private readonly PasswordHasher<CompanyEntity> _passwordHasher;

    public AccountSecurityService(
        ICompanyRepository companyRepository,
        IAccessTokenService accessTokenService,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationService accountVerificationService,
        ILogger<AccountSecurityService> logger)
    {
        _companyRepository = companyRepository;
        _accessTokenService = accessTokenService;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<CompanyEntity>();
    }

    // Email Change Logic
    public async Task<bool> ChangeEmailAsync(EmailChangeRequest emailChangeRequest)
    {
        try
        {
            // Validate the token
            if (!_accessTokenService.IsTokenValid(emailChangeRequest.Token))
            {
                _logger.LogWarning("Invalid token during email change.");
                return false;
            }

            // Retrieve the company ID from the token
            var companyIdString = _accessTokenService.GetUserIdFromToken(emailChangeRequest.Token);
            if (string.IsNullOrEmpty(companyIdString) || !Guid.TryParse(companyIdString, out Guid companyId))
            {
                _logger.LogWarning("Unable to retrieve or parse company ID from token.");
                return false;
            }

            // Retrieve the company from the repository by ID
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company not found for email change.");
                return false;
            }

            // Check if the email already exists in the database
            var existingCompany = await _companyRepository.GetByEmailAsync(emailChangeRequest.Email);
            if (existingCompany != null)
            {
                _logger.LogWarning("Email is already in use.");
                return false;
            }

            // Before changing the email, set IsVerified to false
            company.IsVerified = false;
            await _companyRepository.UpdateAsync(company);

            // Update email and save changes to the repository
            company.Email = emailChangeRequest.Email;
            await _companyRepository.UpdateAsync(company);

            // Now, generate a new account verification token and send the verification email
            var verificationToken = await _accountVerificationTokenService.CreateAccountVerificationTokenAsync(companyId);

            var tokenResult = await _accountVerificationService.SendVerificationEmailAsync(verificationToken);

            if (!tokenResult)
            {
                _logger.LogWarning("Failed to send account verification email.");
                return false;
            }

            _logger.LogInformation("Email successfully changed and verification email sent.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during email change: {ex.Message}");
            return false;
        }
    }

    // Password Change Logic
    public async Task<bool> ChangePasswordAsync(PasswordChangeRequest passwordChangeRequest)
    {
        if (string.IsNullOrWhiteSpace(passwordChangeRequest.Token) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.Password) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid password change request.");
            return false;
        }

        if (passwordChangeRequest.Password != passwordChangeRequest.ConfirmPassword)
        {
            _logger.LogWarning("New password and confirm password do not match.");
            return false;
        }

        try
        {
            // Validate the token (same way as email change)
            if (!_accessTokenService.IsTokenValid(passwordChangeRequest.Token))
            {
                _logger.LogWarning("Invalid token during password change.");
                return false;
            }

            // Retrieve the company ID from the token
            var companyIdString = _accessTokenService.GetUserIdFromToken(passwordChangeRequest.Token);
            if (string.IsNullOrEmpty(companyIdString) || !Guid.TryParse(companyIdString, out Guid companyId))
            {
                _logger.LogWarning("Unable to retrieve or parse company ID from token.");
                return false;
            }

            // Retrieve the company from the repository by ID
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company not found for password change.");
                return false;
            }

            // Hash the new password and update the company's password
            company.PasswordHash = _passwordHasher.HashPassword(company, passwordChangeRequest.Password);
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Password successfully changed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing the password.");
            return false;
        }
    }
}