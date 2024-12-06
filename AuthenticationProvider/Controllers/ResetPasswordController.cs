using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ResetPasswordController : ControllerBase
{
    private readonly IResetPasswordTokenService _resetpasswordtokenService;
    private readonly IResetPasswordService _resetpasswordemailService;

    // Constructor injection for services
    public ResetPasswordController(
        IResetPasswordTokenService resetpasswordtokenService,
        IResetPasswordService resetpasswordemailService)
    {
        _resetpasswordtokenService = resetpasswordtokenService;
        _resetpasswordemailService = resetpasswordemailService;
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
            var resetpasswordtoken = await _resetpasswordtokenService.CreateResetPasswordTokenAsync(companyId);

            // Send the token to the company's registered email
            var emailSent = await _resetpasswordemailService.SendResetPasswordEmailAsync(resetpasswordtoken.Token);

            if (!emailSent)
            {
                return StatusCode(500, new { message = "Failed to send reset password email. Please try again later." });
            }

            return Ok(new { message = "A password reset token has been sent to your email." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
        }
    }

    // Endpoint to reset the password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetpasswordrequest)
    {
        if (resetpasswordrequest == null || string.IsNullOrEmpty(resetpasswordrequest.Token) ||
            string.IsNullOrEmpty(resetpasswordrequest.NewPassword) ||
            string.IsNullOrEmpty(resetpasswordrequest.ConfirmPassword))
        {
            return BadRequest(new { message = "Invalid input data. All fields are required." });
        }

        if (resetpasswordrequest.NewPassword != resetpasswordrequest.ConfirmPassword)
        {
            return BadRequest(new { message = "New password and confirm password do not match." });
        }

        try
        {
            // Validate and retrieve the reset password token
            var resetpasswordtoken = await _resetpasswordtokenService.GetValidResetPasswordTokenAsync(resetpasswordrequest.Token);

            if (resetpasswordtoken == null)
            {
                return BadRequest(new { message = "Invalid or expired reset password token." });
            }

            // Mark the token as used
            await _resetpasswordtokenService.MarkResetPasswordTokenAsUsedAsync(resetpasswordtoken.Id);

            // Reset the company's password (implement password hashing and updating in the database here)
            var passwordResetSuccessful = await _resetpasswordtokenService.ResetCompanyPasswordAsync(
                resetpasswordtoken.Company.Email, resetpasswordrequest.NewPassword);

            if (!passwordResetSuccessful)
            {
                return StatusCode(500, new { message = "Failed to update the password. Please try again later." });
            }

            return Ok(new { message = "Password has been reset successfully." });
        }
        catch (Exception ex)
        {
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
            await _resetpasswordtokenService.DeleteResetPasswordTokensForCompanyAsync(companyId);
            return Ok(new { message = "All reset password tokens for the specified company have been invalidated." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
        }
    }
}
