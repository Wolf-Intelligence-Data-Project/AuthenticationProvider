using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling authentication (sign up and sign in) requests and operations.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signInService;
    private readonly ISignOutService _signOutService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ISignInService signInService,
        ISignOutService signOutService,
        ILogger<AuthController> logger)
    {
        _signInService = signInService ?? throw new ArgumentNullException(nameof(signInService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Endpoint to log in a company by authenticating using the provided credentials.
    /// Returns a token on successful authentication.
    /// </summary>
    /// <param name="request">The credentials to authenticate the company.</param>
    /// <returns>Returns an authentication token if authentication is successful.</returns>
    /// <response code="200">If authentication is successful, returns the authentication token.</response>
    /// <response code="400">If the provided credentials are invalid or the model state is incorrect.</response>
    /// <response code="401">If authentication fails due to invalid credentials.</response>
    /// <response code="500">If an error occurs during authentication.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponses.ModelStateError);
        }

        try
        {
            var response = await _signInService.SignInAsync(request);

            if (response.Success)
            {
                _logger.LogInformation("Login successful for company: {CompanyEmail}", request.Email);
                return Ok(new
                {
                    message = "Inloggning lyckades",
                    token = response.Token
                });
            }

            _logger.LogWarning("Login failed for company: {CompanyEmail}. Invalid credentials.", request.Email);
            return Unauthorized(ErrorResponses.InvalidCredentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for company: {CompanyEmail}.", request.Email);
            return StatusCode(500, ErrorResponses.GeneralError);
        }
    }

    /// <summary>
    /// Endpoint to log out the company by invalidating the current session.
    /// If a token is provided, it will be revoked.
    /// </summary>
    /// <param name="Authorization">Authorization header containing the Bearer token (optional).</param>
    /// <returns>Returns a success message on successful logout.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="400">Bad request if the token is invalid or there is an issue during logout.</response>
    [HttpDelete("logout")]
    public async Task<IActionResult> Logout([FromHeader] string Authorization)
    {
        string token = string.Empty;

        // Check if the token is present and starts with "Bearer"
        if (!string.IsNullOrEmpty(Authorization) && Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = Authorization.Substring(7).Trim();
        }

        // If token is provided, attempt to sign out
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                await _signOutService.SignOutAsync(token);
                _logger.LogInformation("User logged out successfully with token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout attempt with token.");
                return BadRequest(ErrorResponses.TokenExpiredOrInvalid);
            }
        }
        else
        {   // Sign out is always possible, even without token
            _logger.LogWarning("Logout attempted with no token provided.");
        }

        return Ok(new { message = "Utloggning lyckades." });
    }
}
