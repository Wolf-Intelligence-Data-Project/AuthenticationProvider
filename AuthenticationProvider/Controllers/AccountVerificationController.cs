using AuthenticationProvider.Interfaces.Services.Security;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling account verification requests and operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountVerificationController : ControllerBase
{
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly ILogger<AccountVerificationController> _logger;

    public AccountVerificationController(
        IAccountVerificationService accountVerificationService,
        IAccountVerificationTokenService accountVerificationTokenService,
        ILogger<AccountVerificationController> logger)
    {
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a verification email to the account associated with the provided token.
    /// </summary>
    /// <param name="request">Contains the token for account verification.</param>
    /// <returns>
    /// Status message based on the result of the email sending operation:
    /// - 200 OK: If the email was successfully sent.
    /// - 400 Bad Request: If the token is missing or invalid.
    /// - 500 Internal Server Error: If an error occurred while sending the email.
    /// </returns>
    [HttpPost("send-verification-email")]
    public async Task<IActionResult> SendVerificationEmail([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogWarning("Token is missing for the send verification email request.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            var emailSent = await _accountVerificationService.SendVerificationEmailAsync(request.Token);
            if (emailSent != ServiceResult.Success)
            {
                _logger.LogError("Failed to send verification email.");
                return StatusCode(500, ErrorResponses.EmailSendFailure);
            }

            _logger.LogInformation("Verification email sent.");
            return Ok("Verifierings-e-post skickades framgångsrikt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending verification email.");
            return StatusCode(500, ErrorResponses.GeneralError);
        }
    }

    /// <summary>
    /// Verifies the account using the provided verification token.
    /// </summary>
    /// <param name="request">Contains the token for account verification.</param>
    /// <returns>
    /// Status message based on the verification result:
    /// - 200 OK: If the account was successfully verified.
    /// - 400 Bad Request: If the token is invalid or expired.
    /// - 404 Not Found: If the account is not found.
    /// - 500 Internal Server Error: If an error occurs during the verification process.
    /// </returns>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] TokenRequest request)
    {
        // Validate the token
        if (request == null || string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogWarning("Token is missing for the verify email request.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        // Validate the account verification token
        var result = await _accountVerificationTokenService.ValidateAccountVerificationTokenAsync(request);
        if (result is UnauthorizedObjectResult || result is BadRequestObjectResult)
        {
            return result;  // Return error response if token is invalid or expired
        }

        try
        {
            var verificationResult = await _accountVerificationService.VerifyEmailAsync(request.Token);

            // Handle different verification result outcomes
            return verificationResult switch
            {
                ServiceResult.Success => Ok("Kontot verifierades framgångsrikt."),  // Success
                ServiceResult.InvalidToken => BadRequest(ErrorResponses.TokenExpiredOrInvalid),  // Invalid token
                ServiceResult.EmailNotFound => NotFound(ErrorResponses.EmailNotFound),  // Email not found
                ServiceResult.CompanyNotFound => NotFound(ErrorResponses.CompanyNotFound),  // Company not found
                ServiceResult.AlreadyVerified => Ok("Kontot är redan verifierat."),  // Already verified
                ServiceResult.Failure => StatusCode(500, ErrorResponses.VerificationFailed),  // Failure
                _ => StatusCode(500, ErrorResponses.GeneralError)  // General error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying account.");
            return StatusCode(500, ErrorResponses.GeneralError);
        }
    }
}
