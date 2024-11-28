using AuthenticationProvider.Models.SignIn;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AuthenticationProvider.Interfaces.Services;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginSessionController : ControllerBase
{
    private readonly ILoginSessionTokenService _loginSessionTokenService;

    public LoginSessionController(ILoginSessionTokenService loginSessionTokenService)
    {
        _loginSessionTokenService = loginSessionTokenService;
    }

    // Endpoint to generate and store a login token
    [HttpPost("generate-token")]
    public async Task<IActionResult> GenerateLoginToken([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return validation errors
        }

        // Optional: Validate email format before proceeding
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Ogiltigt e-postformat.");
        }

        try
        {
            var token = await _loginSessionTokenService.GenerateLoginSessionTokenAsync(request.Email);
            return Ok(new { token });
        }
        catch (ArgumentException ex)
        {
            // Improve error message handling
            return BadRequest($"Fel vid generering av token: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch other unexpected errors
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }

    // Endpoint to invalidate a login token (logout)
    [HttpPost("invalidate-token")]
    public async Task<IActionResult> InvalidateLoginToken([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return validation errors
        }

        // Optional: Validate email format before proceeding
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Ogiltigt e-postformat.");
        }

        try
        {
            var success = await _loginSessionTokenService.InvalidateLoginSessionTokenAsync(request.Email);
            if (!success)
            {
                return NotFound("Ogiltig e-postadress eller token hittades inte.");
            }

            return Ok("Token ogiltiggjordes framgångsrikt.");
        }
        catch (Exception ex)
        {
            // Catch other unexpected errors
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }

    // Helper method to validate email format
    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
