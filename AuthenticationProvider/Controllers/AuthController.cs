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

    public AuthController(ISignInService signInService)
    {
        _signInService = signInService;
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
}
