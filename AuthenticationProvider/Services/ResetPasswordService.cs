using AuthenticationProvider.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IResetPasswordClient _resetPasswordClient;
    private readonly ILogger<ResetPasswordService> _logger;

    public ResetPasswordService(IResetPasswordClient resetPasswordClient, ILogger<ResetPasswordService> logger)
    {
        _resetPasswordClient = resetPasswordClient ?? throw new ArgumentNullException(nameof(resetPasswordClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a reset password email using the provided token.
    /// </summary>
    /// <param name="token">The reset password token.</param>
    /// <returns>True if the email is sent successfully; otherwise, false.</returns>
    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 10) // Basic token validation
        {
            _logger.LogWarning("Invalid token provided for reset password email.");
            return false;
        }

        try
        {
            bool result = await _resetPasswordClient.SendResetPasswordEmailAsync(token);

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
            return false; // Return false to indicate failure
        }
    }
}
