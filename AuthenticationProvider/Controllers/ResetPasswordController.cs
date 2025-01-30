using AuthenticationProvider.Interfaces.Services.Security;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Dtos;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling password reset requests and operations.
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
    /// Constructor for ResetPasswordController, initializing services and logging.
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

        // Retrieve frontend URL from appsettings.json for redirection after successful token validation.
        _frontendUrl = configuration["FrontendUrl"];
    }

    /// <summary>
    /// Endpoint to request a reset password token by providing an email address.
    /// </summary>
    [HttpPost("request-reset-password-token")]
    public async Task<IActionResult> RequestResetPasswordToken([FromBody] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Email is required.");
            return BadRequest(new
            {
                ErrorCode = "EMAIL_REQUIRED",
                ErrorMessage = "E-postadress krävs.",
                ErrorDetails = "Ingen e-postadress angavs för att begära återställning."
            });
        }

        try
        {
            // Generate a reset password token for the provided email
            var resetPasswordToken = await _resetPasswordTokenService.CreateResetPasswordTokenAsync(email);
            if (string.IsNullOrEmpty(resetPasswordToken))
            {
                return StatusCode(500, ErrorResponses.TokenGenerationFailed);
            }

            // Send the reset password token to the user via email
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
    /// Endpoint to complete the password reset process by providing a valid reset token and new password.
    /// </summary>
    [HttpPost("reset-password/complete")]
    public async Task<IActionResult> CompleteResetPassword([FromBody] ResetPasswordDto resetPasswordRequest)
    {
        if (!ModelState.IsValid || resetPasswordRequest == null ||
            string.IsNullOrEmpty(resetPasswordRequest.Token) ||
            string.IsNullOrEmpty(resetPasswordRequest.NewPassword) ||
            string.IsNullOrEmpty(resetPasswordRequest.ConfirmPassword))
        {
            return BadRequest(ErrorResponses.InvalidInput);
        }

        // Check if the new password matches the confirmation password
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
            // Retrieve the email from the token
            var email = await _resetPasswordTokenService.GetEmailFromTokenAsync(resetPasswordRequest.Token);
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }

            // Reset the password using the provided token and new password
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

            // Validate that the reset password token is valid
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);
            if (resetPasswordToken == null)
            {
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }

            // Mark the token as used
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
    /// Endpoint for displaying the reset password page with a valid token.
    /// Redirects the user to the frontend with the token attached for further processing.
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
            // Validate the token
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
    /// Endpoint to invalidate all reset password tokens for a specific company.
    /// This is typically used for security reasons, such as when a company requests all tokens to be invalidated.
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
            // Invalidate all reset password tokens for the specified company
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
