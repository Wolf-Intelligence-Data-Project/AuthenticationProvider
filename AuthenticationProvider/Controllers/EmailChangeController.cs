using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Interfaces.Services.Tokens;
using Microsoft.AspNetCore.Authorization;
using AuthenticationProvider.Models.Requests;

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
    private readonly IEmailSecurityService _emailChangeService;

    public EmailChangeController(IAccessTokenService accessTokenService, IEmailSecurityService emailChangeService)
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

        if (request == null)
        {
            return BadRequest(ErrorResponses.InvalidInput);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponses.ModelStateError);
        }

        try
        {
            bool result = await _emailChangeService.ChangeEmailAsync(request);

            if (!result)
            {
                return BadRequest(new
                {
                    ErrorCode = "EMAIL_UPDATE_FAILED",
                    ErrorMessage = "E-postadress kan inte uppdateras.",
                    ErrorDetails = "Det gick inte att uppdatera e-postadressen, vänligen försök igen senare."
                });
            }

            return Ok(new { message = "E-postadress har uppdaterats." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
    }
}
