using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AuthenticationProvider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISignInService _signInService;
        private readonly ISignOutService _signOutService;
        private readonly IAccessTokenService _accessTokenService;

        public AuthController(ISignInService signInService, ISignOutService signOutService, IAccessTokenService accessTokenService)
        {
            _signInService = signInService;
            _signOutService = signOutService;
            _accessTokenService = accessTokenService;
        }

        // Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SignInDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input", errors = ModelState });
            }

            var response = await _signInService.SignInAsync(request);
            if (response.Success)
            {
                return Ok(new
                {
                    message = "Login successful",
                    token = response.Token,
                    user = new { response.User.UserName, response.User.Email }
                });
            }

            return Unauthorized(new { message = "Felaktiga inloggningsuppgifter." });
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromHeader] string Authorization)
        {
            // Log the received Authorization header for debugging purposes
            Console.WriteLine($"Received Authorization Header: {Authorization}");

            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(new { message = "No token provided." });
            }

            // Extract token from the Authorization header if it starts with "Bearer "
            var token = Authorization?.StartsWith("Bearer ") == true
                        ? Authorization.Substring(7).Trim() // Extract the token after 'Bearer '
                        : string.Empty;

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Invalid token format." });
            }

            Console.WriteLine($"Extracted Token: {token}");

            // Proceed with token validation...
            // Call your logic for revoking the token and validating it
            _accessTokenService.RevokeAccessToken(token);

            if (!_accessTokenService.IsTokenValid(token))
            {
                return Unauthorized(new { message = "Token is invalid or expired." });
            }

            return Ok(new { message = "Logged out successfully." });
        }


    }
}
