using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Interfaces.Services.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace AuthenticationProvider.Controllers;


/// <summary>
/// Controller responsible for managing password reset operations securely.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ResetPasswordController : ControllerBase
{
    private readonly IResetPasswordTokenService _resetPasswordTokenService;
    private readonly IResetPasswordService _resetPasswordService;
    private readonly ILogger<ResetPasswordController> _logger;
    private readonly string _frontendUrl;

    /// <summary>
    /// Initializes the controller with necessary services and configuration for password reset operations.
    /// </summary>
    public ResetPasswordController(
        IResetPasswordTokenService resetPasswordTokenService,
        IResetPasswordService resetPasswordService,
        ILogger<ResetPasswordController> logger,
        IConfiguration configuration)
    {
        _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
        _resetPasswordService = resetPasswordService ?? throw new ArgumentNullException(nameof(resetPasswordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _frontendUrl = configuration["FrontendUrl"];
    }

    /// <summary>
    /// Initiates the password reset process by securely generating a token.
    /// </summary>
    [HttpPost("request-reset-password-token")]
    public async Task<IActionResult> RequestResetPasswordToken([FromBody] string email)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var resetPasswordToken = await _resetPasswordTokenService.GenerateResetPasswordTokenAsync(email);
            if (string.IsNullOrEmpty(resetPasswordToken))
            {
                return StatusCode(500, ErrorResponses.TokenGenerationFailed);
            }

            var emailSent = await _resetPasswordService.SendResetPasswordEmailAsync(resetPasswordToken);
            if (!emailSent)
            {
                return StatusCode(500, new
                {
                    ErrorCode = "EMAIL_SEND_FAILED",
                    ErrorMessage = "Misslyckades med att skicka e-post för lösenordsåterställning.",
                    ErrorDetails = "Försök igen senare eller kontakta support."
                });
            }

            return Ok(new { message = "En återställningstoken för lösenord har skickats till din e-postadress." });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "ArgumentNullException while requesting reset password token.");
            return BadRequest(ErrorResponses.MissingParameter);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "InvalidOperationException while generating or sending reset password token.");
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while requesting reset password token.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }

    /// <summary>
    /// Verifies the reset password token and redirects to the frontend for further processing.
    /// </summary>
    [Authorize(Policy = "ResetPasswordToken")]
    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPasswordPage([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Reset password link clicked without a token.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        // Extra check of the token before starting a process (where it checks again)
        var isTokenValid = await _resetPasswordTokenService.ValidateResetPasswordTokenAsync(token);
        if (!isTokenValid)
        {
            _logger.LogWarning("Account verification token is invalid, used, or expired.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(token);
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Invalid or expired token accessed.");
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }

            _logger.LogInformation("Valid token accessed");
            return Redirect($"{_frontendUrl}/reset-password?token={token}");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "ArgumentNullException while validating reset password token.");
            return BadRequest(ErrorResponses.MissingParameter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset password token.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }

    /// <summary>
    /// Completes the password reset by verifying the token and updating the password if valid.
    /// </summary>
    [Authorize(Policy = "ResetPasswordToken")]
    [HttpPost("reset-password/complete")]
    public async Task<IActionResult> CompleteResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
    {
        if (!ModelState.IsValid || resetPasswordRequest == null)
        {
            return BadRequest(ErrorResponses.MissingParameter);
        }

        // Extra check of the token before starting a process (where it checks again)
        var isTokenValid = await _resetPasswordTokenService.ValidateResetPasswordTokenAsync(resetPasswordRequest.Token);
        if (!isTokenValid)
        {
            _logger.LogWarning("Account verification token is invalid, used, or expired.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            // Call service method to handle password reset logic
            var success = await _resetPasswordService.ResetPasswordAsync(resetPasswordRequest);

            if (!success)
            {
                return BadRequest(new
                {
                    ErrorCode = "PASSWORD_RESET_FAILED",
                    ErrorMessage = "Failed to reset password.",
                    ErrorDetails = "Please try again later or contact support."
                });
            }

            return Ok(new { message = "Password has been reset successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resetting password.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }
}