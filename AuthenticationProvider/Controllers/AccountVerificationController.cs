using AuthenticationProvider.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountVerificationController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ISendVerificationService _emailVerificationService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;

    public AccountVerificationController(
        ICompanyRepository companyRepository,
        ISendVerificationService emailVerificationService,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationTokenRepository tokenRepository)
    {
        _companyRepository = companyRepository;
        _emailVerificationService = emailVerificationService;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationTokenRepository = tokenRepository;
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token krävs.");
            }

            if (await _accountVerificationTokenService.IsVerificationTokenExpiredAsync(token))
            {
                return BadRequest("Ogiltigt eller utgånget token.");
            }

            var email = ExtractEmailFromToken(token);
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("E-postadress hittades inte i token.");
            }

            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                return BadRequest("Inget företag hittades med den angivna e-postadressen.");
            }

            if (company.IsVerified)
            {
                return BadRequest("Den här e-postadressen är redan verifierad.");
            }

            company.IsVerified = true;
            await _companyRepository.UpdateAsync(company);
            await _accountVerificationTokenService.RevokeVerificationTokenAsync(company.Id);

            return Ok("Din e-postadress har verifierats framgångsrikt.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }

    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                return BadRequest("Företag med denna e-postadress finns inte.");
            }

            if (company.IsVerified)
            {
                return BadRequest("Verifierade användare kan inte begära en ny verifierings-e-post.");
            }

            await _accountVerificationTokenService.RevokeVerificationTokenAsync(company.Id);
            var newToken = await _accountVerificationTokenService.GenerateVerificationTokenAsync(company.Id);

            await _accountVerificationTokenRepository.UpdateLastEmailVerificationTokenAsync(company.Id, newToken);
            var success = await _emailVerificationService.SendVerificationEmailAsync(newToken);
            if (!success)
            {
                return StatusCode(500, "Det gick inte att skicka verifierings-e-posten.");
            }

            return Ok("En ny verifierings-e-post har skickats.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }

    private string ExtractEmailFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var decodedToken = tokenHandler.ReadJwtToken(token);
            var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            return emailClaim?.Value;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Fel vid extrahering av e-postadress från token.", ex);
        }
    }
}