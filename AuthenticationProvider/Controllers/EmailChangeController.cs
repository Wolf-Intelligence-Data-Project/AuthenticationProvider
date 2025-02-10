using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Interfaces.Services.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling secure email change requests and operations for authenticated users.
/// Ensures that only authorized users can request email changes.
/// </summary>
[Authorize(Policy = "AccessToken")]
[Route("api/[controller]")]
[ApiController]

public class EmailChangeController : ControllerBase
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IAccountSecurityService _emailChangeService;

    public EmailChangeController(IAccessTokenService accessTokenService, IAccountSecurityService emailChangeService)
    {
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _emailChangeService = emailChangeService ?? throw new ArgumentNullException(nameof(emailChangeService));
    }

    /// <summary>
    /// Endpoint to securely change the email address for the currently authenticated user.
    /// This operation requires a valid access token for authentication.
    /// </summary>
    /// <returns>Returns a success or failure response depending on the outcome of the email change process.</returns>
    [HttpPatch("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] EmailChangeRequest request)
    {
        // Validate the request for null values or missing information
        if (request == null)
        {
            return BadRequest(ErrorResponses.InvalidInput);
        }

        // Check if the request model is valid
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponses.ModelStateError);
        }

        try
        {
            // Call the service to validate the access token and perform the email change
            bool result = await _emailChangeService.ChangeEmailAsync(request);

            // If the email change fails, return a bad request with specific error details
            if (!result)
            {
                return BadRequest(new
                {
                    ErrorCode = "EMAIL_UPDATE_FAILED",
                    ErrorMessage = "E-postadress kan inte uppdateras.",
                    ErrorDetails = "Det gick inte att uppdatera e-postadressen, vänligen försök igen senare."
                });
            }

            // Return a success message if the email update is successful
            return Ok(new { message = "E-postadress har uppdaterats." });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle unauthorized access attempts (e.g., invalid or expired token)
            return Unauthorized(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (Exception ex)
        {
            // Handle any unexpected errors and log for internal debugging
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
    }
}
