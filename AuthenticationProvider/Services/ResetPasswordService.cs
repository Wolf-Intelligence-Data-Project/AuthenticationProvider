using AuthenticationProvider.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AuthenticationProvider.Models;
using AuthenticationProvider.Data;
using AuthenticationProvider.Repositories;  // Include the CompanyRepository

namespace AuthenticationProvider.Services
{
    public class ResetPasswordService : IResetPasswordService
    {
        private readonly IResetPasswordClient _resetPasswordClient;
        private readonly IResetPasswordTokenService _resetPasswordTokenService;
        private readonly ICompanyRepository _companyRepository;  // Changed to use the ICompanyRepository
        private readonly ILogger<ResetPasswordService> _logger;

        public ResetPasswordService(
            IResetPasswordClient resetPasswordClient,
            IResetPasswordTokenService resetPasswordTokenService,
            ICompanyRepository companyRepository,  // Use company repository to update the password
            ILogger<ResetPasswordService> logger)
        {
            _resetPasswordClient = resetPasswordClient ?? throw new ArgumentNullException(nameof(resetPasswordClient));
            _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository)); // Initialize repository
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
                // Validate the token and ensure it's not expired or used already
                var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token);
                if (resetPasswordToken == null)
                {
                    _logger.LogWarning("Reset password token is invalid or expired.");
                    return false; // Token invalid or expired
                }

                // Send the reset password email
                bool result = await _resetPasswordClient.SendResetPasswordEmailAsync(token);

                if (result)
                {
                    _logger.LogInformation("Reset password email sent successfully.");
                    // Optionally, mark the token as used after successful email send
                    await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id);
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
                    return false; // Token invalid or expired
                }

             
                // Hash the new password before updating
                var hashedPassword = HashPassword(resetPasswordRequest.NewPassword);

                // Update the company's password hash
                var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
                if (company != null)
                {
                    company.PasswordHash = hashedPassword;
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

        /// <summary>
        /// Hashes the password using a secure hashing algorithm.
        /// </summary>
        private string HashPassword(string password)
        {
            // Implement password hashing here (e.g., using bcrypt or another algorithm)
            return password; // Placeholder; use proper hashing logic
        }
    }
}
