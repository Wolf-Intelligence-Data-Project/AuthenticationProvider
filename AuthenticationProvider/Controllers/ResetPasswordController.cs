using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetPasswordController : ControllerBase
    {
        private readonly IResetPasswordTokenService _resetPasswordTokenService;
        private readonly IResetPasswordService _resetPasswordEmailService;
        private readonly ILogger<ResetPasswordController> _logger;

        // Constructor injection for services
        public ResetPasswordController(
            IResetPasswordTokenService resetPasswordTokenService,
            IResetPasswordService resetPasswordEmailService,
            ILogger<ResetPasswordController> logger)
        {
            _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
            _resetPasswordEmailService = resetPasswordEmailService ?? throw new ArgumentNullException(nameof(resetPasswordEmailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Endpoint to request a reset password token
        [HttpPost("request-reset-password-token")]
        public async Task<IActionResult> RequestResetPasswordToken([FromBody] Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid company ID." });
            }

            try
            {
                // Generate a reset password token
                var resetPasswordToken = await _resetPasswordTokenService.CreateResetPasswordTokenAsync(companyId);

                if (string.IsNullOrEmpty(resetPasswordToken))
                {
                    return StatusCode(500, new { message = "Failed to generate reset password token." });
                }

                // Send the token to the company's registered email
                var emailSent = await _resetPasswordEmailService.SendResetPasswordEmailAsync(resetPasswordToken);

                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Failed to send reset password email. Please try again later." });
                }

                return Ok(new { message = "A password reset token has been sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting reset password token.");
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        // Endpoint to reset the password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordRequest)
        {
            if (resetPasswordRequest == null || string.IsNullOrEmpty(resetPasswordRequest.Token) ||
                string.IsNullOrEmpty(resetPasswordRequest.NewPassword) ||
                string.IsNullOrEmpty(resetPasswordRequest.ConfirmPassword))
            {
                return BadRequest(new { message = "Invalid input data. All fields are required." });
            }

            if (resetPasswordRequest.NewPassword != resetPasswordRequest.ConfirmPassword)
            {
                return BadRequest(new { message = "New password and confirm password do not match." });
            }

            try
            {
                // Validate and retrieve the reset password token
                var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);

                if (resetPasswordToken == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset password token." });
                }

                // Mark the token as used
                await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id);

                // Reset the company's password (implement password hashing and updating in the database here)
                var passwordResetSuccessful = await _resetPasswordTokenService.ResetCompanyPasswordAsync(
                    resetPasswordToken.Company.Email, resetPasswordRequest.NewPassword);

                if (!passwordResetSuccessful)
                {
                    return StatusCode(500, new { message = "Failed to update the password. Please try again later." });
                }

                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while resetting password.");
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        // Endpoint to invalidate all reset password tokens for a company
        [HttpPost("invalidate-reset-password-tokens/{companyId}")]
        public async Task<IActionResult> InvalidateResetPasswordTokens(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid company ID." });
            }

            try
            {
                // Delete all reset password tokens for the specified company
                await _resetPasswordTokenService.DeleteResetPasswordTokensForCompanyAsync(companyId);
                return Ok(new { message = "All reset password tokens for the specified company have been invalidated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while invalidating reset password tokens.");
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        // Endpoint to handle GET request for resetting password
        [HttpGet("reset-password")]
        public IActionResult ResetPasswordPage([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            try
            {
                // Validate the token (e.g., check if it's valid and not expired)
                var resetPasswordToken = _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token).Result;

                if (resetPasswordToken == null)
                {
                    return BadRequest(new { message = "Invalid or expired token." });
                }

                // Redirect to appropriate page (or logic here)
                return Redirect("https://www.google.com/"); // Replace with actual logic
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset password token.");
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }
    }
}
