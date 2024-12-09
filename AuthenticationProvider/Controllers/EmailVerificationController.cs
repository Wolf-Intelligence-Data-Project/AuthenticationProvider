using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Tokens;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EmailVerificationController : ControllerBase
{
    private readonly IAccountVerificationService _emailVerificationService;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(
        IAccountVerificationService emailVerificationService,
        ILogger<EmailVerificationController> logger)
    {
        _emailVerificationService = emailVerificationService;
        _logger = logger;
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No token provided for email verification.");
            return BadRequest("Token krävs.");
        }

        try
        {
            // Call the service to verify the email
            var result = await _emailVerificationService.VerifyEmailAsync(token);

            switch (result)
            {
                case VerificationResult.InvalidToken:
                    _logger.LogWarning("Invalid or expired token provided for email verification.");
                    return BadRequest("Ogiltigt eller utgånget token.");
                case VerificationResult.EmailNotFound:
                    _logger.LogWarning("Email not found in the provided token: {Token}", token);
                    return BadRequest("E-postadress hittades inte i token.");
                case VerificationResult.CompanyNotFound:
                    _logger.LogWarning("No company found with the provided email.");
                    return BadRequest("Inget företag hittades med den angivna e-postadressen.");
                case VerificationResult.AlreadyVerified:
                    _logger.LogWarning("The company is already verified.");
                    return BadRequest("E-posten är redan verifierad.");
                case VerificationResult.Success:
                    _logger.LogInformation("Email verified successfully.");
                    return Ok("Din e-postadress har verifierats framgångsrikt.");  // Success message

                default:
                    _logger.LogError("Unknown verification result.");
                    return StatusCode(500, "Ett okänt fel inträffade.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying email for token: {Token}", token);
            return StatusCode(500, "Ett fel inträffade vid verifiering av e-postadress.");
        }
    }
}
