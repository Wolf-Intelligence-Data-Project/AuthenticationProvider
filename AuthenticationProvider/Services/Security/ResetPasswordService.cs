using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Interfaces.Clients;
using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Services.Security;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IResetPasswordClient _resetPasswordClient;
    private readonly IResetPasswordTokenService _resetPasswordTokenService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ResetPasswordService> _logger;
    private readonly PasswordHasher<object> _passwordHasher;

    public ResetPasswordService(
        IResetPasswordClient resetPasswordClient,
        IResetPasswordTokenService resetPasswordTokenService,
        ICompanyRepository companyRepository,
        ILogger<ResetPasswordService> logger)
    {
        _resetPasswordClient = resetPasswordClient ?? throw new ArgumentNullException(nameof(resetPasswordClient));
        _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = new PasswordHasher<object>();
    }

    /// <summary>
    /// Sends a reset password email using the provided token.
    /// </summary>
    /// <param name="token">The reset password token.</param>
    /// <returns>True if the email is sent successfully; otherwise, false.</returns>
    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 10)
        {
            _logger.LogWarning("Invalid token provided for reset password email.");
            return false;
        }

        try
        {
            // Validate the token
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token);
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Reset password token is invalid or expired.");
                return false;
            }

            // Send the reset password email using the client
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
        if (string.IsNullOrWhiteSpace(resetPasswordRequest.Token) ||
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
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);
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

            // Update the company's password hash
            var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
            if (company != null)
            {
                company.PasswordHash = _passwordHasher.HashPassword(null, resetPasswordRequest.NewPassword);
                await _companyRepository.UpdateAsync(company);
            }
            else
            {
                _logger.LogWarning("Company not found for the provided token.");
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
