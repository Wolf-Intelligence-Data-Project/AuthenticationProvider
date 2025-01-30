using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Data.Dtos;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling signup requests and operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly ISignUpService _signUpService;
    private readonly ISignOutService _signOutService;
    private readonly ILogger<SignUpController> _logger;

    public SignUpController(ISignUpService signUpService, ISignOutService signOutService, ILogger<SignUpController> logger)
    {
        _signUpService = signUpService ?? throw new ArgumentNullException(nameof(signUpService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new company in the system.
    /// This endpoint accepts company details for creating a new company record and generating a verification token.
    /// The provided data will be validated, and upon success, a company ID and verification token will be returned.
    /// </summary>
    /// <param name="request">The sign-up request containing the company's details, such as name, email, etc.</param>
    /// <returns>A response containing the company ID and verification token if registration is successful, or an error message if validation fails.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpDto request)
    {
        _logger.LogInformation("Register endpoint called with company data.");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("Model validation failed: {ValidationErrors}", validationErrors);

            return BadRequest(new
            {
                ErrorResponses.ModelStateError.ErrorCode,
                ErrorResponses.ModelStateError.ErrorMessage,
                ErrorDetails = validationErrors
            });
        }

        try
        {
            // Delegate registration logic to the SignUpService
            var signUpResponse = await _signUpService.RegisterCompanyAsync(request);

            _logger.LogInformation("Company registered successfully");

            // Return success message with company ID and verification token
            return Ok(new { message = "Företaget registrerat framgångsrikt!", data = new { companyId = signUpResponse.CompanyId, token = signUpResponse.Token } });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Validation failed during registration: {ErrorMessage}", ex.Message);
            return BadRequest(new
            {
                ErrorCode = "REGISTRATION_FAILED",
                ErrorMessage = "Registreringen misslyckades.",
                ErrorDetails = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }

    /// <summary>
    /// Deletes a company based on the provided company ID.
    /// This endpoint first attempts to sign out the company (invalidate session) and then deletes the company from the system.
    /// It returns a success message or an error if the company was not found or if an issue occurred during deletion.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company to be deleted.</param>
    /// <returns>A success message upon successful deletion, or an error response if the company was not found or deletion failed.</returns>
    [HttpDelete("delete/{companyId}")]
    public async Task<IActionResult> DeleteCompany(Guid companyId)
    {
        _logger.LogInformation("DeleteCompany endpoint called");

        try
        {
            // Sign out the company (invalidate the session)
            var signOutResult = await _signOutService.SignOutAsync(companyId.ToString());
            if (!signOutResult)
            {
                _logger.LogWarning("Failed to sign out the company");
            }

            // Delegate the actual delete logic to the SignUpService
            await _signUpService.DeleteCompanyAsync(companyId);

            _logger.LogInformation("Company with ID deleted successfully.");

            // Return success message
            return Ok(new { message = "Företaget har tagits bort framgångsrikt.", details = (string?)null });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError("Company not found during deletion: {ErrorMessage}", ex.Message);
            return NotFound(ErrorResponses.CompanyNotFound);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Error during company deletion: {ErrorMessage}", ex.Message);
            return BadRequest(new
            {
                ErrorCode = "DELETE_FAILED",
                ErrorMessage = "Misslyckades med att ta bort företaget.",
                ErrorDetails = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during company deletion.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }
}