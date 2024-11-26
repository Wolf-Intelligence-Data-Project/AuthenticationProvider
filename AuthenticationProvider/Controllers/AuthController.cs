using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignIn;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signInService;
    private readonly ISignOutService _signOutService;

    public AuthController(ISignInService signInService, ISignOutService signOutService)
    {
        _signInService = signInService;
        _signOutService = signOutService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // If model is invalid, return validation errors
        }

        var response = await _signInService.SignInAsync(request);
        if (response.Success)
        {
            return Ok(new { Token = response.Token });
        }

        return Unauthorized("Felaktiga inloggningsuppgifter."); // Translation for error message
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token krävs för utloggning."); // Translation for missing token message
        }

        var result = await _signOutService.SignOutAsync(token);
        if (result)
        {
            return Ok("Utloggning lyckades."); // Translation for successful logout
        }

        return BadRequest("Misslyckades med att logga ut. Token kan redan vara ogiltig eller saknas."); // Translation for logout failure
    }
}
