using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Services.Tokens;

namespace AuthenticationProvider.Services.Security;

public class AccountSecurityService : IAccountSecurityService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly ILogger<AccountSecurityService> _logger;
    private readonly PasswordHasher<UserEntity> _passwordHasher;
    private readonly IEmailRestrictionService _emailRestrictionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountSecurityService(
    IUserRepository userRepository,
    IAccessTokenService accessTokenService,
    IAccountVerificationTokenService accountVerificationTokenService,
    IAccountVerificationService accountVerificationService,
    ILogger<AccountSecurityService> logger,
    IConfiguration configuration,
    IEmailRestrictionService emailRestrictionService,
    IHttpContextAccessor httpContextAccessor)  // Add this line
    {
        _userRepository = userRepository;
        _accessTokenService = accessTokenService;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<UserEntity>();
        _emailRestrictionService = emailRestrictionService;
        _httpContextAccessor = httpContextAccessor;  // Assign the injected IHttpContextAccessor
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
            // Ensure valid request parameters
            if (string.IsNullOrWhiteSpace(emailChangeRequest.Email) ||
                string.IsNullOrWhiteSpace(emailChangeRequest.CurrentPassword))
            {
                _logger.LogWarning("Invalid email change request.");
                return false; // Bad request: missing required fields
            }

            // Get token from the cookie (you no longer send it explicitly in the request)
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found in cookies.");
                return false; // No token in cookies
            }

            // Validate token: ensure it is authenticated and the account is verified
            var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(token);

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired.");
                return false;
            }

            if (!isAccountVerified)
            {
                _logger.LogInformation("Account is not verified.");
                return false;
            }

            // Get the user ID from the token
            var userIdString = _accessTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("Unable to retrieve or parse user ID from token.");
                return false;
            }

            // Fetch the user from the database
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for email change.");
                return false;
            }

            // Verify the current password before allowing email change
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, emailChangeRequest.CurrentPassword);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Current password is incorrect.");
                return false;
            }

            // Check if the new email is already in use
            var existingUser = await _userRepository.GetByEmailAsync(emailChangeRequest.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email is already in use.");
                return false;
            }

            // Set the user as unverified before changing the email
            user.IsVerified = false;
            user.Email = emailChangeRequest.Email;
            await _userRepository.UpdateAsync(user);

            // Generate a new verification token and send verification email
            var verificationToken = await _accountVerificationTokenService.GenerateAccountVerificationTokenAsync(userId);
            var tokenResult = await _accountVerificationService.SendVerificationEmailAsync(verificationToken.TokenId);

            if (tokenResult != ServiceResult.Success)
            {
                _logger.LogWarning("Failed to send account verification email.");
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
        // Ensure valid request parameters
        if (
            string.IsNullOrWhiteSpace(passwordChangeRequest.CurrentPassword) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.NewPassword) ||
            string.IsNullOrWhiteSpace(passwordChangeRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid password change request.");
            return false; // Bad request: missing password fields
        }

        try
        {
            // Get token from the cookie
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"];

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found in cookies.");
                return false; // No token in cookies
            }

            // Call ValidateAccessToken to check if the user is authenticated and verified
            var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(token);

            if (!isAuthenticated)
            {
                _logger.LogInformation("Token is invalid or expired.");
                return false;
            }

            if (!isAccountVerified)
            {
                _logger.LogInformation("Token is not verified.");
                return false;
            }

            // Get the user ID from the token
            var userIdString = _accessTokenService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("Unable to retrieve or parse user ID from token.");
                return false;
            }

            // Fetch the user from the database
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
                return false; // Reject request if the current password is wrong
            }

            // Ensure new password and confirm password match
            if (passwordChangeRequest.NewPassword != passwordChangeRequest.ConfirmPassword)
            {
                _logger.LogWarning("New password and confirm password do not match.");
                return false;
            }

            // Hash and update password
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
