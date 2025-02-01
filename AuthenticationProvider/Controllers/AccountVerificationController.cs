using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Interfaces.Repositories;
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
    private readonly ICompanyRepository _companyRepository;
    private readonly string _frontendUrl;

    // Constructor injection of services
    public AccountVerificationController(
        IAccountVerificationService accountVerificationService,
        IAccountVerificationTokenService accountVerificationTokenService,
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationController> logger,
        IConfiguration configuration)
    {
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _companyRepository = companyRepository;
        _logger = logger;
        _frontendUrl = configuration["VerificationSuccess"];
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
    /// Verifies the account using the provided verification token and redirects to the frontend on success.
    /// </summary>
    /// <param name="token">The token for account verification, provided as a query parameter.</param>
    /// <returns>
    /// - 200 OK: If the account was successfully verified and the user is redirected to the frontend.
    /// - 400 Bad Request: If the token is invalid or expired.
    /// - 404 Not Found: If the account or company is not found.
    /// - 500 Internal Server Error: If an error occurs during the verification process.
    /// </returns>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        // Validate the token
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is missing for the verify email request.");
            return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
        }

        // Validate the account verification token
        var result = await _accountVerificationTokenService.ValidateAccountVerificationTokenAsync(new TokenRequest { Token = token });
        if (result is UnauthorizedObjectResult || result is BadRequestObjectResult)
        {
            return result;  // Return error response if token is invalid or expired
        }

        try
        {
            // Call VerifyEmailAsync
            var verificationResult = await _accountVerificationService.VerifyEmailAsync(token);

            // Handle different verification result outcomes
            return verificationResult switch
            {
                ServiceResult.Success => Redirect(_frontendUrl),  // Redirect to frontend on success
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
    public async Task<IActionResult> ResendVerificationEmail([FromBody] EmailRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            _logger.LogWarning("No email provided for resending verification email.");
            return BadRequest(new { message = "Email is required." });
        }

        try
        {
            // Delegate the business logic to the service
            var result = await _accountVerificationService.ResendVerificationEmailAsync(request.Email);

            if (result != ServiceResult.Success)
            {
                return StatusCode(500, ErrorResponses.InternalServerError);
            }

            // Return success response
            return Ok(new { message = "Verification email resent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending the verification email for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while resending the verification email." });
        }
    }
}
