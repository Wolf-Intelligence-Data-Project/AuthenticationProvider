using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.SignUp;
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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);  // Return validation errors
        }

        try
        {
            var response = await _signUpService.RegisterCompanyAsync(request);
            return Ok(response); // Successfully registered
        }
        catch (ArgumentException ex)
        {
            // Catch specific argument-related errors
            return BadRequest(new { message = $"Felaktiga argument: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            // Handle operation-specific exceptions
            return BadRequest(new { message = $"Ogiltig operation: {ex.Message}" });
        }
        catch (Exception ex)
        {
            // Catch other unexpected exceptions
            return StatusCode(500, new { message = $"Ett oväntat fel inträffade: {ex.Message}" });
        }
    }
}
