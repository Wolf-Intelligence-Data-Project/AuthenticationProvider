using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Dtos;
using AuthenticationProvider.Models.Responses;
using Azure.Core;

namespace AuthenticationProvider.Services.Security;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IEmailVerificationTokenService _emailVerificationTokenService;
    private readonly IEmailVerificationClient _emailVerificationClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IEmailVerificationTokenService emailVerificationTokenService,
        IEmailVerificationClient emailVerificationClient,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _emailVerificationClient = emailVerificationClient;
        _emailVerificationTokenService = emailVerificationTokenService;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;      
    }

    public async Task<ServiceResult> PrepareAndSendVerificationAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                throw new ArgumentException("Invalid user ID format");
            }

            var tokenInfo = await _emailVerificationTokenService.GenerateEmailVerificationTokenAsync(userGuid);

            var verification = new EmailVerificationDto
            {
                VerificationId = tokenInfo.TokenId
            };

            bool result = await _emailVerificationClient.SendVerificationEmailAsync(verification);

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

    public async Task<ServiceResult> VerifyEmailAsync(string verificationId)
    {
        if (string.IsNullOrEmpty(verificationId))
        {
            _logger.LogWarning("No token provided for email verification.");
            return ServiceResult.InvalidToken; 
        }

        var emailVerificationToken = await _emailVerificationTokenService.GetValidEmailVerificationTokenAsync(verificationId);
        if (emailVerificationToken == null)
        {
            _logger.LogWarning("The verification token is invalid.");
            return ServiceResult.InvalidToken;
        }

        var userId = emailVerificationToken.UserId;

        var user = await _userRepository.GetByIdAsync(userId); 
        if (user == null)
        {
            _logger.LogWarning("User not found.");
            return ServiceResult.UserNotFound;
        }

        if (user.IsVerified)
        {
            _logger.LogInformation("The user is already verified.");
            return ServiceResult.AlreadyVerified; 
        }

        user.IsVerified = true;
        await _userRepository.UpdateAsync(user);

        if (!Guid.TryParse(verificationId, out Guid verificationGuid))
        {
            throw new ArgumentException("Invalid verification ID format");
        }

        await _emailVerificationTokenService.MarkEmailVerificationTokenAsUsedAsync(verificationGuid);

        _logger.LogInformation("Email verified successfully.");
        return ServiceResult.Success; 
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
            _logger.LogWarning("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaAAAAAAAAAAAAAAAAAAAA");
            _logger.LogWarning(email);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", email);
                return ServiceResult.Failure; 
            }
            var userId = user.UserId;
            _logger.LogInformation("User found with UserId: {UserId}", userId);
            // Set the user as not verified
            user.IsVerified = false;
            await _userRepository.UpdateAsync(user);

            var tokenInfo = await _emailVerificationTokenService.GenerateEmailVerificationTokenAsync(user.UserId);

            var verification = new EmailVerificationDto
            {
                VerificationId = tokenInfo.TokenId
            };

            bool result = await _emailVerificationClient.SendVerificationEmailAsync(verification);

            if (!result)
            {
                _logger.LogError("Failed to send email.");
                return ServiceResult.InvalidToken;
            }

            _logger.LogInformation("Verification email resent to user with email: {Email}", email);

            return ServiceResult.Success; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending the verification email for email: {Email}", email);
            return ServiceResult.Failure;  
        }
    }
}
