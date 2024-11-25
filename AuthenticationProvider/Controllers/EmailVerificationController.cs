using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;


namespace AuthenticationProvider.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailVerificationController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ITokenService _tokenService;

        public EmailVerificationController(ICompanyRepository companyRepository, ITokenService tokenService)
        {
            _companyRepository = companyRepository;
            _tokenService = tokenService;
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is required.");
            }

            // Log the received token for debugging purposes
            Console.WriteLine($"Received token: {token}");

            // Token validation logic (using your TokenService to validate the token)
            var claimsPrincipal = _tokenService.ValidateToken(token);
            if (claimsPrincipal == null)
            {
                return BadRequest("Invalid or expired token.");
            }

            // Extract email using different methods
            string email = ExtractEmailFromToken(token) ??
                           ExtractEmailFromClaimsPrincipal(claimsPrincipal) ??
                           GetEmailFromJwtToken(token);

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email not found in token.");
            }

            // Log the email for debugging purposes
            Console.WriteLine($"Extracted Email from Token: {email}");

            // Fetch the company using the email
            var company = await _companyRepository.GetByEmailAsync(email);

            if (company == null)
            {
                return BadRequest($"No company found with this email: {email}");
            }

            company.IsVerified = true;
            await _companyRepository.UpdateAsync(company);

            return Ok("Your email has been successfully verified.");
        }

        // Method to extract email directly from the token
        private string ExtractEmailFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var decodedToken = tokenHandler.ReadJwtToken(token);

            // Access the 'sub' claim directly
            var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            return emailClaim?.Value;
        }

        // Method to extract email by manually checking all claims
        private string ExtractEmailFromClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
        {
            // Check if claimsPrincipal has any claims
            if (claimsPrincipal?.Claims == null || !claimsPrincipal.Claims.Any())
            {
                return null;  // No claims available, return null
            }

            // Log all claims for debugging
            foreach (var claim in claimsPrincipal.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
            }

            // Attempt to get the email claim manually by searching through all claims
            var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("sub", System.StringComparison.OrdinalIgnoreCase));

            return emailClaim?.Value;
        }

        // Method to extract email directly from JwtSecurityToken
        private string GetEmailFromJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Access the 'sub' claim directly from the JwtSecurityToken
            var emailClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

            return emailClaim?.Value;
        }
    }
}
