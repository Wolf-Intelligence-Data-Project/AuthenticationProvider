using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;

using Microsoft.AspNetCore.Mvc;

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
            var resetPasswordToken = await _resetPasswordTokenService.CreateResetPasswordTokenAsync(email);
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
    /// Completes the password reset by verifying the token and updating the password if valid.
    /// </summary>
    [HttpPost("reset-password/complete")]
    public async Task<IActionResult> CompleteResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
    {
        if (!ModelState.IsValid || resetPasswordRequest == null ||
            string.IsNullOrEmpty(resetPasswordRequest.Token) ||
            string.IsNullOrEmpty(resetPasswordRequest.NewPassword) ||
            string.IsNullOrEmpty(resetPasswordRequest.ConfirmPassword))
        {
            return BadRequest(ErrorResponses.InvalidInput);
        }

        if (resetPasswordRequest.NewPassword != resetPasswordRequest.ConfirmPassword)
        {
            return BadRequest(new
            {
                ErrorCode = "PASSWORD_MISMATCH",
                ErrorMessage = "Lösenord och bekräfta lösenord matchar inte.",
                ErrorDetails = "Var vänlig och ange samma lösenord i båda fälten."
            });
        }

        try
        {
            var email = await _resetPasswordTokenService.GetEmailFromTokenAsync(resetPasswordRequest.Token);
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }

            var passwordResetSuccessful = await _resetPasswordService.ResetPasswordAsync(resetPasswordRequest);
            if (!passwordResetSuccessful)
            {
                return StatusCode(500, new
                {
                    ErrorCode = "PASSWORD_RESET_FAILED",
                    ErrorMessage = "Misslyckades med att uppdatera lösenordet.",
                    ErrorDetails = "Försök igen senare eller kontakta support."
                });
            }

            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);
            if (resetPasswordToken == null)
            {
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }

            await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id);

            return Ok(new { message = "Lösenordet har återställts framgångsrikt." });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "ArgumentNullException while resetting password.");
            return BadRequest(ErrorResponses.MissingParameter);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "InvalidOperationException while resetting password.");
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while resetting password.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }

    /// <summary>
    /// Verifies the reset password token and redirects to the frontend for further processing.
    /// </summary>
    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPasswordPage([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Reset password link clicked without a token.");
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
    /// Revokes all active reset password tokens for a specified company for security purposes.
    /// </summary>
    [HttpPost("invalidate-reset-password-tokens/{companyId}")]
    public async Task<IActionResult> InvalidateResetPasswordTokens(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(ErrorResponses.CompanyNotFound);
        }

        try
        {
            await _resetPasswordTokenService.DeleteResetPasswordTokensForCompanyAsync(companyId);
            return Ok(new { message = "Alla återställningstoken för lösenord för det angivna företaget har ogiltigförklarats." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while invalidating reset password tokens.");
            return StatusCode(500, ErrorResponses.TokenInvalidationError);
        }
    }
}
