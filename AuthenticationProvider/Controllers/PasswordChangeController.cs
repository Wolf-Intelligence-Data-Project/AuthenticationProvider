using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling password change requests and operations.
/// </summary>
[Route("api/[controller]")]
[ApiController]

public class PasswordChangeController : ControllerBase
{
    private readonly IEmailSecurityService _emailSecurityService;
    private readonly ILogger<PasswordChangeController> _logger;

    public PasswordChangeController(IEmailSecurityService emailSecurityService, ILogger<PasswordChangeController> logger)
    {
        _emailSecurityService = emailSecurityService ?? throw new ArgumentNullException(nameof(emailSecurityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePasswordAsync()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage);
            _logger.LogWarning($"Model validation failed: {string.Join("; ", errors)}");

            return BadRequest(new { errors });
        }
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        string rawBody = await reader.ReadToEndAsync();

        _logger.LogInformation($"Raw Request Body: {rawBody}");

        PasswordChangeRequest request;
        try
        {
            request = JsonSerializer.Deserialize<PasswordChangeRequest>(rawBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize request body.");
            return BadRequest("Invalid JSON format.");
        }

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
            bool result = await _emailSecurityService.ChangePasswordAsync(request);

            if (result)
            {
                return Ok(new { message = "Lösenordet har ändrats." });
            }
            else
            {
                return BadRequest(new
                {
                    ErrorCode = "PASSWORD_CHANGE_FAILED",
                    ErrorMessage = "Lösenordsändring misslyckades.",
                    ErrorDetails = "Failed to change password, check if the token is correct or has expired."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid ändring av lösenord.");
            return StatusCode(500, ErrorResponses.InternalServerError);
        }
    }
}
