using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignUp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly ISignUpService _signUpService;
    private readonly ILogger<SignUpController> _logger;

    public SignUpController(ISignUpService signUpService, ILogger<SignUpController> logger)
    {
        _signUpService = signUpService;
        _logger = logger;
    }

    // POST: /api/SignUp/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpRequest request)
    {
        _logger.LogInformation("Register endpoint called.");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed.");
            return BadRequest(ModelState);  // Return validation errors
        }

        try
        {
            // Delegate registration logic to the SignUpService
            var signUpResponse = await _signUpService.RegisterCompanyAsync(request);

            _logger.LogInformation("Company registered successfully with ID {CompanyId}.", signUpResponse.CompanyId);

            return Ok(new
            {
                message = "Company registered successfully!",
                companyId = signUpResponse.CompanyId,  // Returning the Company ID as well
                token = signUpResponse.Token  // Returning the verification token
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Validation failed during registration: {ErrorMessage}", ex.Message);
            return BadRequest(new { message = ex.Message });  // Handle validation-specific errors
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during registration: {ErrorMessage}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }

    // DELETE: /api/SignUp/delete/{companyId}
    [HttpDelete("delete/{companyId}")]
    public async Task<IActionResult> DeleteCompany(Guid companyId)
    {
        _logger.LogInformation("DeleteCompany endpoint called for Company ID {CompanyId}.", companyId);

        try
        {
            // Delegate delete logic to the SignUpService
            await _signUpService.DeleteCompanyAsync(companyId);

            _logger.LogInformation("Company with ID {CompanyId} deleted successfully.", companyId);

            return Ok(new
            {
                message = "Company deleted successfully!"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError("Company not found during deletion: {ErrorMessage}", ex.Message);
            return NotFound(new { message = "Company not found." });  // Handle company not found errors
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during company deletion: {ErrorMessage}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }
}
