using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly ISignUpService _signUpService;

    public SignUpController(ISignUpService signUpService)
    {
        _signUpService = signUpService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpRequest request)
    {
        try
        {
            var response = await _signUpService.RegisterCompanyAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
