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
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AccountVerificationService> _logger;
    private readonly IConfiguration _configuration;

    public AccountVerificationService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationClient accountVerificationClient,
        IUserRepository userRepository,
        ILogger<AccountVerificationService> logger,
        IConfiguration configuration)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _accountVerificationClient = accountVerificationClient;
        _accountVerificationTokenService = accountVerificationTokenService;
        _userRepository = userRepository;
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
                _logger.LogWarning("The email in the token does not match any user.");
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

        // Extract userId from the accountVerificationToken
        var userId = accountVerificationToken.UserId;

        var user = await _userRepository.GetByIdAsync(userId);  // Fetch the user using userId
        if (user == null)
        {
            _logger.LogWarning("User not found.");
            return ServiceResult.UserNotFound;  // Handle case when user is not found
        }

        if (user.IsVerified)
        {
            _logger.LogInformation("The user is already verified.");
            return ServiceResult.AlreadyVerified;  // Already verified, no further action needed
        }

        user.IsVerified = true;
        await _userRepository.UpdateAsync(user);  // Update the user as verified

        // Mark the account verification token as used after the user is successfully verified
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
            // Fetch the user by email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", email);
                return ServiceResult.Failure;  // Use Failure property directly
            }

            // Set the user as not verified
            user.IsVerified = false;
            await _userRepository.UpdateAsync(user);

            // Generate a new account verification token for the user
            var newToken = await _accountVerificationTokenService.GenerateAccountVerificationTokenAsync(user.UserId);
            if (string.IsNullOrEmpty(newToken))
            {
                _logger.LogError("Failed to create a new verification token for user with email: {Email}", email);
                return ServiceResult.Failure;  // Use Failure property directly
            }

            // Send the new verification token to the email verification provider
            var emailSent = await SendVerificationEmailAsync(newToken);
            if (emailSent != ServiceResult.Success)
            {
                return ServiceResult.Failure;  // Use Failure property directly
            }

            _logger.LogInformation("Verification email resent to user with email: {Email}", email);

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

            // Extract the email claim from the token
            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(emailClaim))
            {
                _logger.LogWarning("No email claim found in the token.");
                return false;
            }
            // Check if a user exists with the extracted email
            var user = await _userRepository.GetByEmailAsync(emailClaim);
            if (user == null)
            {
                _logger.LogWarning("No user found with the email from the token.");
                return false;
            }

            // Check if the user is already verified (optional)
            if (user.IsVerified)
            {
                _logger.LogInformation("The user is already verified.");
                return false; // If already verified, return false or handle as needed
            }

            return true; // Email claim exists, matches a user, and user is not verified yet
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating email from token.");
            return false;
        }
    }
}
