using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;

namespace AuthenticationProvider.Services;

public class SendVerificationService : ISendVerificationService
{
    private readonly ISendVerificationClient _emailVerificationClient;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly ICompanyRepository _companyRepository;

    public SendVerificationService(
        ISendVerificationClient emailVerificationClient,
        IAccountVerificationTokenService accountVerificationTokenService,
        ICompanyRepository companyRepository)
    {
        _emailVerificationClient = emailVerificationClient;
        _accountVerificationTokenService = accountVerificationTokenService;
        _companyRepository = companyRepository;
    }

    public async Task<bool> SendVerificationEmailAsync(string email)
    {
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(company.LastEmailVerificationToken))
        {
            await _accountVerificationTokenService.RevokeVerificationTokenAsync(company.Id); // Revoke the existing token
        }

        var newToken = await _accountVerificationTokenService.GenerateVerificationTokenAsync(company.Id);

        var tokenSent = await _emailVerificationClient.SendVerificationEmailAsync(newToken);
        if (!tokenSent)
        {
            return false;
        }

        await _companyRepository.UpdateEmailVerificationTokenAsync(company.Email, newToken);
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var isExpired = await _accountVerificationTokenService.IsVerificationTokenExpiredAsync(token);
        if (isExpired)
        {
            return false;
        }

        string email = ExtractEmailFromToken(token);
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            return false;
        }

        if (company.LastEmailVerificationToken != token)
        {
            return false;
        }

        company.IsVerified = true;
        company.LastEmailVerificationToken = string.Empty;
        await _companyRepository.UpdateAsync(company);

        return true;
    }

    private string ExtractEmailFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var decodedToken = tokenHandler.ReadJwtToken(token);
        var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == "sub");
        return emailClaim?.Value;
    }
}