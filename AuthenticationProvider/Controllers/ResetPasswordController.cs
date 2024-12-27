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
        public async Task<IActionResult> RequestResetPasswordToken([FromBody] string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            try
            {
                // Generate a reset password token using the provided email
                var resetPasswordToken = await _resetPasswordTokenService.CreateResetPasswordTokenAsync(Email);

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


        // Change the GET route for validating reset password token to a unique one
        [HttpGet("validate-reset-password-token")]
        public async Task<IActionResult> ValidateResetPasswordToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            try
            {
                // Validate and retrieve the reset password token
                var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token);

                if (resetPasswordToken == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset password token." });
                }

                // If token is valid, redirect to frontend page with token
                return Redirect($"https://yourfrontend.com/reset-password?token={token}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while validating reset password token.");
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }

        [HttpGet("reset-password")]
        public async Task<IActionResult> ResetPasswordPage([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Reset password link clicked without a token.");
                return BadRequest(new { message = "Token is required." });
            }

            try
            {
                // Log the token and timestamp for tracking
                _logger.LogInformation("Reset password link clicked with token: {Token} at {Timestamp}", token, DateTime.UtcNow);

                // Validate the token (e.g., check if it's valid and not expired)
                var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token);

                if (resetPasswordToken == null)
                {
                    _logger.LogWarning("Invalid or expired token accessed: {Token} at {Timestamp}", token, DateTime.UtcNow);
                    return BadRequest(new { message = "Invalid or expired token." });
                }

                // Log successful token validation
                _logger.LogInformation("Valid token accessed: {Token} at {Timestamp}", token, DateTime.UtcNow);

                // If the token is valid, redirect to the frontend page with the token
                return Redirect($"http://localhost:3000/reset-password?token={token}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset password token: {Token} at {Timestamp}", token, DateTime.UtcNow);
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }


        [HttpPost("reset-password/complete")]
        public async Task<IActionResult> CompleteResetPassword([FromBody] ResetPasswordDto resetPasswordRequest)
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
                // Get email from the token
                var email = await _resetPasswordTokenService.GetEmailFromTokenAsync(resetPasswordRequest.Token);
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Invalid or expired reset password token." });
                }

                // Reset the company's password
                var passwordResetSuccessful = await _resetPasswordTokenService.ResetCompanyPasswordAsync(email, resetPasswordRequest.NewPassword);

                if (!passwordResetSuccessful)
                {
                    return StatusCode(500, new { message = "Failed to update the password. Please try again later." });
                }

                // Assuming resetPasswordRequest.Token is a string token (JWT or reset password token string)
                var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);

                if (resetPasswordToken == null)
                {
                    return BadRequest(new { message = "Invalid or expired reset password token." });
                }

                // Mark the token as used using its Guid
                await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id); // resetPasswordToken.Id is a Guid

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
    }
}
