using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignUp;
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
        // Check for validation errors in the incoming request model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return validation errors as part of the response
        }

        try
        {
            var response = await _signUpService.RegisterCompanyAsync(request);
            return Ok(response); // Successfully registered
        }
        catch (Exception ex)
        {
            // Return a generic error message for exceptions
            return BadRequest(new { message = ex.Message });
        }
    }

}
