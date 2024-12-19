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
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(new { message = "No token provided." });
            }

            var token = Authorization.StartsWith("Bearer ") ? Authorization.Substring(7) : Authorization;

            // Revoke the token first
            _accessTokenService.RevokeAccessToken(token);

            // Now, proceed to validate or return success
            if (!_accessTokenService.IsTokenValid(token))
            {
                return Unauthorized(new { message = "Token is invalid or expired." });
            }

            return Ok(new { message = "Logged out successfully." });
        }




    }
}
