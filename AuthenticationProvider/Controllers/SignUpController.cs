using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignUp;
using Microsoft.AspNetCore.Mvc;
using System;
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
            // Delegate registration logic to the SignUpService
            var signUpResponse = await _signUpService.RegisterCompanyAsync(request);

            return Ok(new
            {
                message = "Company registered successfully!",
                token = signUpResponse.Token
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });  // Handle exceptions
        }
    }
}
