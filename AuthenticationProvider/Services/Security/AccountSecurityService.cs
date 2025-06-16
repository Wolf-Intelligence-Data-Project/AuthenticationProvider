using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Responses;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Requests;

namespace AuthenticationProvider.Services.Security;

public class EmailSecurityService : IEmailSecurityService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IEmailVerificationTokenService _emailVerificationTokenService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<EmailSecurityService> _logger;
    private readonly PasswordHasher<UserEntity> _passwordHasher;
    private readonly IEmailRestrictionService _emailRestrictionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmailSecurityService(
    IUserRepository userRepository,
    IAccessTokenService accessTokenService,
    IEmailVerificationTokenService emailVerificationTokenService,
    IEmailVerificationService emailVerificationService,
    ILogger<EmailSecurityService> logger,
    IConfiguration configuration,
    IEmailRestrictionService emailRestrictionService,
    IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _accessTokenService = accessTokenService;
        _emailVerificationTokenService = emailVerificationTokenService;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<UserEntity>();
        _emailRestrictionService = emailRestrictionService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Changes the email address of a user after validating the token and email restrictions.
    /// </summary>
    /// <param name="emailChangeRequest">The request containing the new email and the token for authentication.</param>
    /// <returns>True if the email is successfully changed, false otherwise.</returns>
    public async Task<bool> ChangeEmailAsync(EmailChangeRequest emailChangeRequest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(emailChangeRequest.Email) ||
                string.IsNullOrWhiteSpace(emailChangeRequest.CurrentPassword))
            {
                _logger.LogWarning("Invalid email change request.");
                return false;
            }

            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found in cookies.");
                return false;
            }

            var (isAuthenticated, isEmailVerified) = _accessTokenService.ValidateAccessToken(token);

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired.");
                return false;
            }

            if (!isEmailVerified)
            {
                _logger.LogInformation("Email is not verified.");
                return false;
            }

            var userIdString = _accessTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("Unable to retrieve or parse user ID from token.");
                return false;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for email change.");
                return false;
            }

            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, emailChangeRequest.CurrentPassword);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Current password is incorrect.");
                return false;
            }

            var existingUser = await _userRepository.GetByEmailAsync(emailChangeRequest.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email is already in use.");
                return false;
            }

            user.IsVerified = false;
            user.Email = emailChangeRequest.Email;
            await _userRepository.UpdateAsync(user);

            var verificationToken = await _emailVerificationTokenService.GenerateEmailVerificationTokenAsync(userId);
            var tokenResult = await _emailVerificationService.PrepareAndSendVerificationAsync(verificationToken.TokenId);

            if (tokenResult != ServiceResult.Success)
            {
                _logger.LogWarning("Failed to send email verification email.");
                return false;
            }

            _logger.LogInformation("Email successfully changed and verification email sent.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing the email.");
            return false;
        }
    }

    /// <summary>
    /// Changes the password for a user after validating the token and password match.
    /// </summary>
    /// <param name="passwordChangeRequest">The request containing the new password, confirmation, and token for authentication.</param>
    /// <returns>True if the password is successfully changed, false otherwise.</returns>
    public async Task<bool> ChangePasswordAsync(PasswordChangeRequest passwordChangeRequest)
    {
        if (
            string.IsNullOrWhiteSpace(passwordChangeRequest.CurrentPassword) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.NewPassword) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid password change request.");
            return false;
        }

        try
        {
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"];

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found in cookies.");
                return false;
            }

            var (isAuthenticated, isEmailVerified) = _accessTokenService.ValidateAccessToken(token);

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired.");
                return false;
            }

            if (!isEmailVerified)
            {
                _logger.LogInformation("Token is not verified.");
                return false;
            }

            var userIdString = _accessTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("Unable to retrieve or parse user ID from token.");
                return false;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for password change.");
                return false;
            }

            // Verify the current password before proceeding
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, passwordChangeRequest.CurrentPassword);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Current password is incorrect.");
                return false;
            }

            if (passwordChangeRequest.NewPassword != passwordChangeRequest.ConfirmPassword)
            {
                _logger.LogWarning("New password and confirm password do not match.");
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, passwordChangeRequest.NewPassword);
            await _userRepository.UpdateAsync(user);

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
