using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignIn;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signInService;
    private readonly ISignOutService _signOutService;
    private readonly IAccessTokenService _accessTokenService;

    public AuthController(ISignInService signInService, ISignOutService signOutService, IAccessTokenService accessTokenService)
    {
        _signInService = signInService;
        _signOutService = signOutService;
        _accessTokenService = accessTokenService;
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
            // Return the token generated in the SignInService
            return Ok(new { Token = response.Token });
        }

        return Unauthorized("Felaktiga inloggningsuppgifter."); // Translation for error message
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token krävs för utloggning.");
        }

        // Revoke the access token using AccessTokenService
        _accessTokenService.RevokeAccessToken(token);

        var result = await _signOutService.SignOutAsync(token);
        if (result)
        {
            return Ok("Utloggning lyckades.");
        }

        return BadRequest("Misslyckades med att logga ut. Token kan redan vara ogiltig eller saknas.");
    }

}
