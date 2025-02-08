using AuthenticationProvider.Interfaces.Security;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
    private readonly IAccessTokenService _accessTokenService;
    private readonly ICaptchaVerificationService _captchaService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthController> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 3;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(
        ISignInService signInService,
        ISignOutService signOutService,
        IAccessTokenService accessTokenService,
        ICaptchaVerificationService captchaService,
        IMemoryCache cache,
        ILogger<AuthController> logger,
        IHttpContextAccessor httpContextAccessor) // Add IHttpContextAccessor here
    {
        _signInService = signInService ?? throw new ArgumentNullException(nameof(signInService));
        _signOutService = signOutService ?? throw new ArgumentNullException(nameof(signOutService));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _cache = cache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor)); // Initialize the field
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
                _logger.LogInformation("Inloggning lyckades för: {CompanyEmail}", request.Email);

                // The token is already stored in the cookie inside GenerateAccessToken
                return Ok(new { message = "Inloggning lyckades" }); // No need to return token
            }

            failedAttempts++;
            _cache.Set(cacheKey, failedAttempts, TimeSpan.FromMinutes(LockoutDurationMinutes));

            if (failedAttempts >= MaxFailedAttempts)
            {
                return Unauthorized("För många försök. CAPTCHA krävs för nästa försök.");
            }

            _logger.LogWarning("Inloggning misslyckades för: {CompanyEmail}", request.Email);
            return Unauthorized("Ogiltiga inloggningsuppgifter.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid inloggningsförsök för: {CompanyEmail}", request.Email);
            return StatusCode(500, "Ett internt fel uppstod.");
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
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["accessToken"];

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Access token is missing." });
        }

        var signOutSuccess = await _signOutService.SignOutAsync(token);

        if (!signOutSuccess)
        {
            return StatusCode(500, new { message = "Logout failed due to an internal error." });
        }

        // Clear the HttpOnly cookie by setting an expiration date in the past
        Response.Cookies.Append("AccessToken", "", new CookieOptions
        {
            Expires = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddDays(-1), // Expire the cookie
            HttpOnly = true,                       // Ensure it's HttpOnly
            Secure = true,                         // Secure if using HTTPS
            SameSite = SameSiteMode.Strict,        // SameSite setting
        });

        // No need to clear cookie here again, it's handled by RevokeAccessToken.
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Endpoint to check the login status of the company based on the current authentication token.
    /// </summary>
    /// <returns>Returns a success message if the company is authenticated, or an error if not.</returns>
    /// <response code="200">If the company is authenticated.</response>
    /// <response code="401">If the company is not authenticated.</response>
    [HttpGet("status")]
    public IActionResult GetAuthStatus()
    {
        var token = Request.Cookies["AccessToken"];  // Get the JWT from the HttpOnly cookie

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Access token is missing." });
        }

        var (isAuthenticated, isAccountVerified) = _accessTokenService.ValidateAccessToken(token);

        return Ok(new { isAuthenticated, isAccountVerified });
    }
}
