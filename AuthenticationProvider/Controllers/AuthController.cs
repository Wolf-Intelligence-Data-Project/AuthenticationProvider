using AuthenticationProvider.Interfaces.Security;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Handles authentication operations such as login, logout, and checking authentication status.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signInService;
    private readonly ISignOutService _signOutService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ICaptchaVerificationService _captchaService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthController> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 3;

    public AuthController(
        ISignInService signInService,
        ISignOutService signOutService,
        IAccessTokenService accessTokenService,
        ICaptchaVerificationService captchaService,
        IMemoryCache cache,
        ILogger<AuthController> logger)
    {
        _signInService = signInService ?? throw new ArgumentNullException(nameof(signInService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _cache = cache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a company using the provided credentials and returns an access token upon success.
    /// </summary>
    /// <param name="request">The sign-in request containing login credentials.</param>
    /// <returns>Returns a success message if authentication succeeds, otherwise an error response.</returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="400">Invalid input data.</response>
    /// <response code="401">Authentication failed due to incorrect credentials.</response>
    /// <response code="500">Internal server error during authentication.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Ogiltiga data.");
        }

        var cacheKey = $"failed_attempts_{request.Email}";
        var failedAttempts = _cache.Get<int>(cacheKey);

        if (failedAttempts >= MaxFailedAttempts)
        {
            if (string.IsNullOrWhiteSpace(request.CaptchaToken))
            {
                return BadRequest("CAPTCHA krävs efter för många misslyckade försök.");
            }

            if (!await _captchaService.VerifyCaptchaAsync(request.CaptchaToken))
            {
                return BadRequest("Ogiltig CAPTCHA-verifiering.");
            }
        }

        try
        {
            var response = await _signInService.SignInAsync(request);

            if (response.Success)
            {
                _cache.Remove(cacheKey);
                _logger.LogInformation("Login successful for: {CompanyEmail}", request.Email);

                return Ok(new { message = "Inloggning lyckades." });
            }

            failedAttempts++;
            _cache.Set(cacheKey, failedAttempts, TimeSpan.FromMinutes(LockoutDurationMinutes));

            if (failedAttempts >= MaxFailedAttempts)
            {
                return Unauthorized("För många försök. CAPTCHA krävs för nästa försök.");
            }

            _logger.LogWarning("Login failed for: {CompanyEmail}", request.Email);
            return Unauthorized("Ogiltiga inloggningsuppgifter.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for: {CompanyEmail}", request.Email);
            return StatusCode(500, "Ett internt fel uppstod.");
        }
    }

    /// <summary>
    /// Logs out the currently authenticated company by revoking the access token.
    /// </summary>
    /// <returns>Returns a success message upon successful logout.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="400">Access token is missing.</response>
    /// <response code="500">Internal server error during logout.</response>
    [HttpDelete("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["accessToken"];

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Åtkomsttoken saknas." });
        }

        var signOutSuccess = await _signOutService.SignOutAsync(token);

        if (!signOutSuccess)
        {
            _logger.LogError("Logout failed due to an internal error.");
            return StatusCode(500, new { message = "Utloggning misslyckades på grund av ett internt fel." });
        }

        Response.Cookies.Append("AccessToken", "", new CookieOptions
        {
            Expires = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddDays(-1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });

        _logger.LogInformation("User successfully logged out.");
        return Ok(new { message = "Utloggning lyckades." });
    }

    /// <summary>
    /// Checks the authentication status of the currently logged-in company.
    /// </summary>
    /// <returns>Returns authentication and verification status.</returns>
    /// <response code="200">Returns authentication status.</response>
    /// <response code="400">Access token is missing.</response>
    [HttpGet("status")]
    public IActionResult GetAuthStatus()
    {
        var token = Request.Cookies["AccessToken"];

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Åtkomsttoken saknas." });
        }

        var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(token);

        return Ok(new { isAuthenticated, isAccountVerified });
    }
}
