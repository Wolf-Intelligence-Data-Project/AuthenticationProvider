using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly ISignUpService _signUpService;
    private readonly ISignOutService _signOutService; // Inject SignOutService
    private readonly ILogger<SignUpController> _logger;

    public SignUpController(ISignUpService signUpService, ISignOutService signOutService, ILogger<SignUpController> logger)
    {
        _signUpService = signUpService ?? throw new ArgumentNullException(nameof(signUpService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService)); // Initialize SignOutService
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new company. This endpoint takes company details and registers it.
    /// </summary>
    /// <param name="request">The sign-up details for the company.</param>
    /// <returns>Returns the result of the registration, including the company ID and a verification token.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpDto request)
    {
        _logger.LogInformation("Register endpoint called with company data.");

        if (!ModelState.IsValid) // Validate input model
        {
            var validationErrors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("Model validation failed: {ValidationErrors}", validationErrors);
            return BadRequest(new { message = "Ogiltig indata", errors = validationErrors });
        }

        try
        {
            // Delegate registration logic to the SignUpService
            var signUpResponse = await _signUpService.RegisterCompanyAsync(request);

            _logger.LogInformation("Company registered successfully with ID {CompanyId}.", signUpResponse.CompanyId);

            return Ok(new
            {
                message = "Företaget registrerat framgångsrikt!",
                companyId = signUpResponse.CompanyId,
                token = signUpResponse.Token // Returning the company verification token
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Validation failed during registration: {ErrorMessage}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration.");
            return StatusCode(500, new { message = "Ett oväntat fel inträffade.", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a company based on the provided company ID.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company to delete.</param>
    /// <returns>Returns the result of the deletion, including success or failure message.</returns>
    [HttpDelete("delete/{companyId}")]
    public async Task<IActionResult> DeleteCompany(Guid companyId)
    {
        _logger.LogInformation("DeleteCompany endpoint called for Company ID {CompanyId}.", companyId);

        try
        {
            // Sign out the company (invalidate the session)
            var signOutResult = await _signOutService.SignOutAsync(companyId.ToString()); // Assuming the token is based on company ID or similar identifier
            if (!signOutResult)
            {
                _logger.LogWarning("Failed to sign out the company with ID {CompanyId} before deletion.", companyId);
            }

            // Delegate the actual delete logic to the SignUpService
            await _signUpService.DeleteCompanyAsync(companyId);

            _logger.LogInformation("Company with ID {CompanyId} deleted successfully.", companyId);

            // Return success message
            return Ok(new { message = "Företaget har tagits bort framgångsrikt." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError("Company not found during deletion: {ErrorMessage}", ex.Message);
            return NotFound(new { message = "Företaget hittades inte." }); // Handle company not found error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during company deletion.");
            return StatusCode(500, new { message = "Ett oväntat fel inträffade.", details = ex.Message });
        }
    }
}
