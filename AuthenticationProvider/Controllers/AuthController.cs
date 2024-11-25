using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuthenticationProvider.Services;
using System.Threading.Tasks;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignIn;

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
        var response = await _signInService.SignInAsync(request);
        if (response.Success)
        {
            return Ok(new { Token = response.Token });
        }

        return Unauthorized(response.ErrorMessage);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token is required for logout.");
        }

        var result = await _signOutService.SignOutAsync(token);
        if (result)
        {
            return Ok("Successfully logged out.");
        }

        return BadRequest("Failed to log out. Token might already be invalid or missing.");
    }

}
