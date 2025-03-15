using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;

namespace AuthenticationProvider.Services.Security;

public class AccountVerificationService : IAccountVerificationService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationClient _accountVerificationClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountVerificationService> _logger;

    public AccountVerificationService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationClient accountVerificationClient,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AccountVerificationService> logger)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _accountVerificationClient = accountVerificationClient;
        _accountVerificationTokenService = accountVerificationTokenService;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;      
    }

    public async Task<ServiceResult> SendVerificationEmailAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                throw new ArgumentException("Invalid user ID format");
            }

            var tokenInfo = await _accountVerificationTokenService.GenerateAccountVerificationTokenAsync(userGuid);

            var verification = new SendVerificationRequest
            {
                VerificationId = tokenInfo.TokenId  // Ensure we are using TokenId here
            };

            bool result = await _accountVerificationClient.SendVerificationEmailAsync(verification);

            if (!result)
            {
                _logger.LogError("Failed to send email.");
                return ServiceResult.InvalidToken; 
            }

            return ServiceResult.Success; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email");
            return ServiceResult.InvalidToken;
        }
    }

    public async Task<ServiceResult> VerifyAccountAsync(string token)
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

        if (!Guid.TryParse(token, out Guid verificationGuid))
        {
            throw new ArgumentException("Invalid verification ID format");
        }
        // Mark the account verification token as used after the user is successfully verified
        await _accountVerificationTokenService.MarkAccountVerificationTokenAsUsedAsync(verificationGuid);

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
            if (newToken == null)
            {
                _logger.LogError("Failed to create a new verification token for user with email: {Email}", email);
                return ServiceResult.Failure;  // Use Failure property directly
            }

            // Send the new verification token to the email verification provider
            var emailSent = await SendVerificationEmailAsync(newToken.TokenId);
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
}
