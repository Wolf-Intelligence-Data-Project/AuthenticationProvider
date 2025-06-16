using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Requests;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling email verification requests and operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailVerificationController : ControllerBase
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IEmailVerificationTokenService _emailVerificationTokenService;
    private readonly IUserRepository _userRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _frontendUrl;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(
        IEmailVerificationService emailVerificationService,
        IEmailVerificationTokenService emailVerificationTokenService,
        IUserRepository userRepository,
        IAccessTokenService accessTokenService,
        ILogger<EmailVerificationController> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _emailVerificationTokenService = emailVerificationTokenService;
        _emailVerificationService = emailVerificationService;
        _userRepository = userRepository;
        _accessTokenService = accessTokenService;
        _httpContextAccessor = httpContextAccessor;
        _frontendUrl = configuration["VerificationSuccess"]!;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_frontendUrl))
        {
            _logger.LogError("Frontend URL for verification success is not configured.");
        }
    }

    /// <summary>
    /// Verifies the email using the provided verification token and redirects to the frontend on success.
    /// </summary>
    /// <param name="verification">The token for email verification, provided as a query parameter.</param>
    /// <returns>
    /// - 200 OK: If the email was successfully verified and the user is redirected to the frontend.
    /// - 400 Bad Request: If the token is invalid or expired.
    /// - 404 Not Found: If the email or user is not found.
    /// - 500 Internal Server Error: If an error occurs during the verification process.
    /// </returns>
    //[Authorize(Policy = "EmailVerificationPolicy")]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string verification)
    {
        _logger.LogWarning("Token is missing for the verify email request.");
        // Validate the token
        if (string.IsNullOrWhiteSpace(verification))
        {
            _logger.LogWarning("Token is missing for the verify email request.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            var verificationResult = await _emailVerificationService.VerifyEmailAsync(verification);

            if (verificationResult == ServiceResult.Success)
            {
                try
                {
                    return Redirect(_frontendUrl);
                }
                catch
                {
                    return Ok("Kontot har verifierats framgångsrikt. Du kan nu logga in.");
                }
            }

            return verificationResult switch
            {
                ServiceResult.InvalidToken => BadRequest(ErrorResponses.TokenExpiredOrInvalid),
                ServiceResult.EmailNotFound => NotFound(ErrorResponses.EmailNotFound),
                ServiceResult.UserNotFound => NotFound(ErrorResponses.UserNotFound),
                ServiceResult.AlreadyVerified => Ok("Kontot är redan verifierat."),
                ServiceResult.Failure => StatusCode(500, ErrorResponses.VerificationFailed),
                _ => StatusCode(500, ErrorResponses.GeneralError)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying email.");
            return StatusCode(500, ErrorResponses.GeneralError);
        }
    }

    /// <summary>
    /// Resends the verification email to the provided email address if it has not been verified yet.
    /// </summary>
    /// <param name="request">The email request containing the email address to which the verification email should be sent.</param>
    /// <returns>
    /// - 200 OK: If the verification email was successfully resent.
    /// - 400 Bad Request: If no email address is provided in the request.
    /// - 500 Internal Server Error: If an error occurs during the process of resending the verification email.
    /// </returns>
    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            _logger.LogWarning("No email provided for resending verification email.");
            return BadRequest(new { message = "Email is required." });
        }

        try
        {
            _logger.LogWarning(request.Email);
            var result = await _emailVerificationService.ResendVerificationEmailAsync(request.Email);

            if (result != ServiceResult.Success)
            {
                return StatusCode(500, ErrorResponses.InternalServerError);
            }

            return Ok(new { message = "Verification email resent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending the verification email for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while resending the verification email." });
        }
    }

    /// <summary>
    /// Sends a verification email to the email associated with the provided token.
    /// </summary>
    /// <param name="request">Contains the token for email verification.</param>
    /// <returns>
    /// Status message based on the result of the email sending operation:
    /// - 200 OK: If the email was successfully sent.
    /// - 400 Bad Request: If the token is missing or invalid.
    /// - 500 Internal Server Error: If an error occurred while sending the email.
    /// </returns>
}
