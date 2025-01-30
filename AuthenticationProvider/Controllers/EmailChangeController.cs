using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling email change requests and operations.
/// </summary>
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
    /// Endpoint to change the email address for a user.
    /// </summary>
    /// <param name="request">EmailChangeRequest containing new email details.</param>
    /// <returns>Returns success or failure message based on the outcome.</returns>
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
            // Delegate token validation and email change logic to services
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
