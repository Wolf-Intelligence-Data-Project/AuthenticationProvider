using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthenticationProvider.Controllers;

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
            return BadRequest("Token krävs.");
        }

        // Validate the token
        var claimsPrincipal = _tokenService.ValidateToken(token);
        if (claimsPrincipal == null)
        {
            return BadRequest("Ogiltigt eller utgånget token."); 
        }

        string email = ExtractEmailFromToken(token) ??
                       ExtractEmailFromClaimsPrincipal(claimsPrincipal);

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("E-postadress hittades inte i token.");
        }

        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            return BadRequest("Inget företag hittades med den angivna e-postadressen."); 
        }

        company.IsVerified = true;
        await _companyRepository.UpdateAsync(company);

        return Ok("Din e-postadress har verifierats framgångsrikt.");
    }

    private string ExtractEmailFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var decodedToken = tokenHandler.ReadJwtToken(token);
        var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        return emailClaim?.Value;
    }

    private string ExtractEmailFromClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.Claims
            .FirstOrDefault(c => c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }
}
