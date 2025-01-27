using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PasswordChangeController : ControllerBase
{
    private readonly IAccountSecurityService _accountSecurityService;
    private readonly ILogger<PasswordChangeController> _logger;

    public PasswordChangeController(IAccountSecurityService passwordChangeService, ILogger<PasswordChangeController> logger)
    {
        _accountSecurityService = passwordChangeService ?? throw new ArgumentNullException(nameof(passwordChangeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Change password for the company using a valid token.
    /// </summary>
    /// <param name="request">The password change request.</param>
    /// <returns>Success or failure result.</returns>
    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordChangeRequest request)
    {
        if (request == null)
        {
            return BadRequest("Invalid request.");
        }

        try
        {
            // Delegate validation and business logic to the service
            bool result = await _accountSecurityService.ChangePasswordAsync(request);

            if (result)
            {
                return Ok("Password successfully changed.");
            }
            else
            {
                return BadRequest("Password change failed. Please verify your token and try again.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing the password.");
            return StatusCode(500, "Internal server error.");
        }
    }
}
