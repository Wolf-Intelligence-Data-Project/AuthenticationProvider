using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Requests;
using System.IdentityModel.Tokens.Jwt;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Services.Security.Clients;

namespace AuthenticationProvider.Services.Security;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IResetPasswordClient _resetPasswordClient;
    private readonly IResetPasswordTokenService _resetPasswordTokenService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResetPasswordService> _logger;
    private readonly PasswordHasher<object> _passwordHasher;

    public ResetPasswordService(
        IResetPasswordClient resetPasswordClient,
        IResetPasswordTokenService resetPasswordTokenService,
        IUserRepository userRepository,
        ILogger<ResetPasswordService> logger)
    {
        _resetPasswordClient = resetPasswordClient ?? throw new ArgumentNullException(nameof(resetPasswordClient));
        _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = new PasswordHasher<object>();
    }

    public async Task CreateResetPasswordTokenAsync(string email)
    {
        try
        {
            var resetPasswordTokenInfo = await _resetPasswordTokenService.GenerateResetPasswordTokenAsync(email);

            // Now you can access both TokenId and TokenString from the resetPasswordTokenInfo object
            string resetId = resetPasswordTokenInfo.TokenId;
            string token = resetPasswordTokenInfo.TokenString;

            if (resetPasswordTokenInfo == null)
            {
                _logger.LogWarning("Token could not be generated.");
            }

            var emailSent = await SendResetPasswordEmailAsync(token, resetId);
            if (!emailSent)
            {
                _logger.LogWarning("Token could not be transferred/sent further.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Misslyckades med att skicka e-post för lösenordsåterställning.");
            throw new InvalidOperationException("Ett oväntat fel uppstod, försök igen senare.");
        }
    }


    /// <summary>
    /// Sends a reset password email using the provided token.
    /// </summary>
    /// <param name="token">The reset password token.</param>
    /// <returns>True if the email is sent successfully; otherwise, false.</returns>
    private async Task<bool> SendResetPasswordEmailAsync(string token, string resetId)
{
    if (string.IsNullOrWhiteSpace(token) || token.Length < 10 || resetId == null || token == null)
    {
        _logger.LogWarning("Invalid token provided for reset password email.");
        return false;
    }

    try
    {
        // Validate the email in the token
        //if (!await EmailValidation(token))
        //{
        //    _logger.LogWarning("The email in the token does not match any user.");
        //    return false;
        //}

        // Validate the reset password token
        var resetPasswordToken = await _resetPasswordTokenService.ValidateResetPasswordTokenAsync(resetId);
        if (!resetPasswordToken)
        {
            _logger.LogWarning("Reset password token is invalid or expired.");
            return false;
        }

        // Create ResetPasswordEmailRequest using the resetId
        var resetRequest = new SendResetPasswordRequest
        {
            ResetId = resetId // Use the resetId as VerificationId
        };

        // Send the reset password email using the client
        bool result = await _resetPasswordClient.SendResetPasswordEmailAsync(resetRequest);

        if (result)
        {
            _logger.LogInformation("Reset password email sent successfully.");
        }
        else
        {
            _logger.LogWarning("Failed to send reset password email.");
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "An error occurred while sending the reset password email.");
        return false;
    }
}

    /// <summary>
    /// Handles the actual password reset when the user submits the new password.
    /// </summary>
    /// <param name="resetPasswordRequest">The request containing the token and the new password.</param>
    /// <returns>True if the password is successfully reset; otherwise, false.</returns>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
    {
        if (string.IsNullOrWhiteSpace(resetPasswordRequest.ResetId) ||
            string.IsNullOrWhiteSpace(resetPasswordRequest.NewPassword) ||
            string.IsNullOrWhiteSpace(resetPasswordRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid reset password request.");
            return false;
        }

        if (resetPasswordRequest.NewPassword != resetPasswordRequest.ConfirmPassword)
        {
            _logger.LogWarning("New password and confirm password do not match.");
            return false;
        }

        try
        {

            // Validate the reset password token
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.ResetId);
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Reset password token is invalid or expired.");
                return false;
            }

            // Additional validation logic
            if (resetPasswordToken.IsUsed)
            {
                _logger.LogWarning("The reset password token has already been used.");
                return false;
            }

            // Update the user's password hash
            var user = await _userRepository.GetByIdAsync(resetPasswordToken.UserId);
            if (user != null)
            {
                user.PasswordHash = _passwordHasher.HashPassword(null, resetPasswordRequest.NewPassword);
                await _userRepository.UpdateAsync(user);
            }
            else
            {
                _logger.LogWarning("User not found for the provided token.");
                return false;
            }

            // Mark the token as used
            await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id);

            _logger.LogInformation("Password reset successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resetting the password.");
            return false;
        }
    }
}
