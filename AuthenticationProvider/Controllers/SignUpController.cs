using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Requests;
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
    /// Registers a new user in the system.
    /// This endpoint accepts user details for creating a new user record and generating a verification token.
    /// The provided data will be validated, and upon success, a user ID and verification token will be returned.
    /// </summary>
    /// <param name="request">The sign-up request containing the user's details, such as name, email, etc.</param>
    /// <returns>A response containing the user ID and verification token if registration is successful, or an error message if validation fails.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpRequest request)
    {
        _logger.LogInformation("Register endpoint called with user data.");

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
            var signUpResponse = await _signUpService.RegisterUserAsync(request);

            _logger.LogInformation("User registered successfully");

            // Return success message with user ID and verification token
            return Ok(new { message = "Användaren registrerat framgångsrikt!", data = new { userId = signUpResponse.UserId, token = signUpResponse.Token } });
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
    /// Deletes a user based on the provided user ID.
    /// This endpoint first attempts to sign out the user (invalidate session) and then deletes the user from the system.
    /// It returns a success message or an error if the user was not found or if an issue occurred during deletion.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to be deleted.</param>
    /// <returns>A success message upon successful deletion, or an error response if the user was not found or deletion failed.</returns>
    [HttpDelete("delete-user")]
    public async Task<IActionResult> DeleteUser(DeleteRequest deleteRequest)
    {
        _logger.LogInformation("DeleteUser endpoint called");

        try
        {
            // Delete logic to the SignUpService
            await _signUpService.DeleteUserAsync(deleteRequest);

            _logger.LogInformation("User with ID deleted successfully.");

            // Return success message
            return Ok(new { message = "Användaren har tagits bort framgångsrikt.", details = (string?)null });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError("User not found during deletion: {ErrorMessage}", ex.Message);
            return NotFound(ErrorResponses.UserNotFound);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Error during User deletion: {ErrorMessage}", ex.Message);
            return BadRequest(new
            {
                ErrorCode = "DELETE_FAILED",
                ErrorMessage = "Misslyckades med att ta bort företaget.",
                ErrorDetails = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user deletion.");
            return StatusCode(500, ErrorResponses.GeneralInternalError(ex));
        }
    }
}