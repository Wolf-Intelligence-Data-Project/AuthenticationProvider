using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ResetPasswordController : ControllerBase
{
    private readonly IResetPasswordTokenService _resetPasswordTokenService;
    private readonly IResetPasswordService _resetPasswordEmailService;
    private readonly ILogger<ResetPasswordController> _logger;

    // Constructor injection for the required services and logger
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
        // Validate the ModelState to ensure the input is correct
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Ogiltig modellstatus." });
        }

        // Ensure the email is provided
        if (string.IsNullOrEmpty(Email))
        {
            return BadRequest(new { message = "E-postadress krävs." }); 
        }

        try
        {
            // Generate a reset password token using the provided email
            var resetPasswordToken = await _resetPasswordTokenService.CreateResetPasswordTokenAsync(Email);

            if (string.IsNullOrEmpty(resetPasswordToken))
            {
                return StatusCode(500, new { message = "Misslyckades med att generera återställningstoken för lösenord." }); 
            }

            // Send the token to the company's registered email
            var emailSent = await _resetPasswordEmailService.SendResetPasswordEmailAsync(resetPasswordToken);

            if (!emailSent)
            {
                return StatusCode(500, new { message = "Misslyckades med att skicka e-post för lösenordsåterställning. Försök igen senare." }); 
            }

            return Ok(new { message = "En återställningstoken för lösenord har skickats till din e-postadress." }); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while requesting reset password token.");
            return StatusCode(500, new { message = "Ett fel inträffade vid behandlingen av din begäran.", details = ex.Message });
        }
    }

    // Endpoint to complete the password reset
    [HttpPost("reset-password/complete")]
    public async Task<IActionResult> CompleteResetPassword([FromBody] ResetPasswordDto resetPasswordRequest)
    {
        // Validate the ModelState to ensure input is correct
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Ogiltig modellstatus." });
        }

        // Check if the request contains necessary data
        if (resetPasswordRequest == null || string.IsNullOrEmpty(resetPasswordRequest.Token) ||
            string.IsNullOrEmpty(resetPasswordRequest.NewPassword) ||
            string.IsNullOrEmpty(resetPasswordRequest.ConfirmPassword))
        {
            return BadRequest(new { message = "Ogiltig indata. Alla fält är obligatoriska." });
        }

        // Ensure new password and confirm password match
        if (resetPasswordRequest.NewPassword != resetPasswordRequest.ConfirmPassword)
        {
            return BadRequest(new { message = "Lösenord och bekräfta lösenord matchar inte." });
        }

        try
        {
            // Get email from the token (this could be a JWT or other reset token)
            var email = await _resetPasswordTokenService.GetEmailFromTokenAsync(resetPasswordRequest.Token);
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Ogiltig eller utgången återställningstoken för lösenord." });
            }

            // Reset the company's password with the new password
            var passwordResetSuccessful = await _resetPasswordTokenService.ResetCompanyPasswordAsync(email, resetPasswordRequest.NewPassword);

            if (!passwordResetSuccessful)
            {
                return StatusCode(500, new { message = "Misslyckades med att uppdatera lösenordet. Försök igen senare." });
            }

            // Validate the reset password token (e.g., check if it's valid and not expired)
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);

            if (resetPasswordToken == null)
            {
                return BadRequest(new { message = "Ogiltig eller utgången återställningstoken för lösenord." });
            }

            // Mark the token as used to prevent reuse
            await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id); // resetPasswordToken.Id is a Guid

            return Ok(new { message = "Lösenordet har återställts framgångsrikt." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resetting password.");
            return StatusCode(500, new { message = "Ett fel inträffade vid återställning av lösenord.", details = ex.Message });
        }
    }

    // Endpoint to handle the reset password page request (with a token query parameter)
    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPasswordPage([FromQuery] string token)
    {
        // Ensure the token is provided
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Reset password link clicked without a token.");
            return BadRequest(new { message = "Token krävs." });
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
                return BadRequest(new { message = "Ogiltig eller utgången token." });
            }

            // Log successful token validation
            _logger.LogInformation("Valid token accessed: {Token} at {Timestamp}", token, DateTime.UtcNow);

            // If the token is valid, redirect to the frontend page with the token
            return Redirect($"http://localhost:3001" +
                $"/reset-password?token={token}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset password token: {Token} at {Timestamp}", token, DateTime.UtcNow);
            return StatusCode(500, new { message = "Ett fel inträffade vid validering av återställningstoken för lösenord.", details = ex.Message });
        }
    }

    // Endpoint to invalidate all reset password tokens for a company
    [HttpPost("invalidate-reset-password-tokens/{companyId}")]
    public async Task<IActionResult> InvalidateResetPasswordTokens(Guid companyId)
    {
        // Ensure the companyId is valid
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { message = "Ogiltigt företags-ID." });
        }

        try
        {
            // Delete all reset password tokens for the specified company
            await _resetPasswordTokenService.DeleteResetPasswordTokensForCompanyAsync(companyId);
            return Ok(new { message = "Alla återställningstoken för lösenord för det angivna företaget har ogiltigförklarats." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while invalidating reset password tokens.");
            return StatusCode(500, new { message = "Ett fel inträffade vid invalidisering av återställningstoken för lösenord.", details = ex.Message });
        }
    }
}
