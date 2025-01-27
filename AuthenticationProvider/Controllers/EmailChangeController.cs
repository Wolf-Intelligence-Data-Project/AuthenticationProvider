using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

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

    [HttpPatch("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] EmailChangeRequest request)
    {
        if (request == null)
        {
            return BadRequest("Invalid request.");
        }

        try
        {
            // Delegate token validation and email change logic to services
            bool result = await _emailChangeService.ChangeEmailAsync(request);

            if (!result)
            {
                return BadRequest("E-postadress kan inte uppdateras.");
            }

            return Ok("E-postadress har uppdaterats.");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Ogiltig eller utgången token.");
        }
        catch (Exception ex)
        {
            // Log the exception (if you have logging configured)
            return StatusCode(500, "Ett internt fel uppstod.");
        }
    }
}
