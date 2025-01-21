using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signInService;
    private readonly ISignOutService _signOutService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ILogger<AuthController> _logger;

    // Constructor dependency injection
    public AuthController(
        ISignInService signInService,
        ISignOutService signOutService,
        IAccessTokenService accessTokenService,
        ILogger<AuthController> logger)
    {
        _signInService = signInService ?? throw new ArgumentNullException(nameof(signInService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Login endpoint. Authenticates the company using credentials provided in the SignInDto.
    /// </summary>
    /// <param name="request">SignInDto containing the company credentials (username/email and password).</param>
    /// <returns>Returns an authentication token if successful, or a message indicating failure.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Ogiltig indata", errors = ModelState });
        }

        try
        {
            var response = await _signInService.SignInAsync(request);
            if (response.Success)
            {
                _logger.LogInformation("Login successful for user: {UserName}", response.User.UserName);

                return Ok(new
                {
                    message = "Inloggning lyckades",
                    token = response.Token,
                    user = new { response.User.UserName, response.User.Email }
                });
            }

            _logger.LogWarning("Login failed for user: {UserName}", request.Email); // Log failed login attempt
            return Unauthorized(new { message = "Felaktiga inloggningsuppgifter" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for user: {Email}", request.Email);
            return StatusCode(500, new { message = "Ett fel inträffade vid inloggning." });
        }
    }

    /// <summary>
    /// Logout endpoint. Invalidates the access token and logs the user out.
    /// </summary>
    /// <param name="Authorization">Authorization header containing the Bearer token.</param>
    /// <returns>Returns a success message if logout is successful, or an error message.</returns>
    [HttpPost("logout")]
    public IActionResult Logout([FromHeader] string Authorization)
    {
        // Check if the Authorization header is provided
        if (string.IsNullOrEmpty(Authorization))
        {
            _logger.LogWarning("Logout attempt with no Authorization token.");
            return BadRequest(new { message = "Ingen token tillhandahölls." });
        }

        // Extract the token from the Authorization header
        var token = Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? Authorization.Substring(7).Trim()
                    : string.Empty;

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Logout attempt with invalid token format.");
            return BadRequest(new { message = "Ogiltigt tokenformat." });
        }

        try
        {
            // Log the token being revoked (for internal tracking purposes, ensure this is safe in production)
            _logger.LogInformation("Logout attempt with token: {Token}", token);

            // Revoke the token and check if it is valid
            _accessTokenService.RevokeAccessToken(token);

            if (!_accessTokenService.IsTokenValid(token))
            {
                _logger.LogInformation("Token is invalid or expired: {Token}", token);
                return Unauthorized(new { message = "Token är ogiltigt eller har löpt ut." });
            }

            return Ok(new { message = "Utloggning lyckades." });
        }
        catch (Exception ex)
        {
            // Log any exception during the logout process
            _logger.LogError(ex, "Error during logout attempt for token: {Token}", token);
            return StatusCode(500, new { message = "Ett fel inträffade vid utloggning." });
        }
    }
}
