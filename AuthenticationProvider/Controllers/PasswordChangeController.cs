using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling password change requests and operations.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PasswordChangeController : ControllerBase
{
    private readonly IAccountSecurityService _accountSecurityService;
    private readonly ILogger<PasswordChangeController> _logger;

    public PasswordChangeController(IAccountSecurityService accountSecurityService, ILogger<PasswordChangeController> logger)
    {
        _accountSecurityService = accountSecurityService ?? throw new ArgumentNullException(nameof(accountSecurityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Change the password for the company using a valid token provided in the request.
    /// </summary>
    /// <param name="request">The password change request containing the new password and token.</param>
    /// <returns>Returns an action result indicating whether the password change was successful or not.</returns>
    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordChangeRequest request)
    {
        // Check if request is null
        if (request == null)
        {
            return BadRequest(ErrorResponses.InvalidInput);
        }

        // Check if the model state is valid
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponses.ModelStateError);
        }

        try
        {
            // Change the password
            bool result = await _accountSecurityService.ChangePasswordAsync(request);

            if (result)
            {
                return Ok(new { message = "Lösenordet har ändrats." });
            }
            else
            {
                _logger.LogWarning("Lösenordsändring misslyckades: Token ogiltig eller utgången.");
                return BadRequest(new
                {
                    ErrorCode = "PASSWORD_CHANGE_FAILED",
                    ErrorMessage = "Lösenordsändring misslyckades.",
                    ErrorDetails = "Failed to change password, check if the token is correct or has expired."
                });
            }

        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "En nödvändig parameter saknas vid lösenordsändring.");
            return BadRequest(ErrorResponses.MissingParameter);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Ogiltig eller utgången token.");
            return Unauthorized(ErrorResponses.UnauthorizedAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid ändring av lösenord.");
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
    }
}
