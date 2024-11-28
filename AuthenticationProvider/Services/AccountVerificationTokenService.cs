using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;

namespace AuthenticationProvider.Services;

public class AccountVerificationTokenService : IAccountVerificationTokenService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<AccountVerificationTokenService> _logger;

    public AccountVerificationTokenService(
        IAccountVerificationTokenRepository tokenRepository,
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationTokenService> logger)
    {
        _accountVerificationTokenRepository = tokenRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<string> GenerateVerificationTokenAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Företags-ID är tomt.");
        }

        var company = await _companyRepository.GetByGuidAsync(companyId);
        if (company != null && company.IsVerified)
        {
            throw new InvalidOperationException("Företaget är redan verifierat.");
        }

        await RevokeVerificationTokenAsync(companyId); // Revoke any existing token

        var token = GenerateNewToken(companyId); // Implement this method to generate the token
        await _accountVerificationTokenRepository.SaveEmailVerificationTokenAsync(companyId, token);

        _logger.LogInformation($"Verifieringstoken genererad för Företags-ID: {companyId}");
        return token;
    }

    public async Task<bool> RevokeVerificationTokenAsync(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            _logger.LogWarning("Försökte återkalla token för ett tomt Företags-ID.");
            return false;
        }

        var company = await _companyRepository.GetByGuidAsync(companyId);
        if (company != null && company.IsVerified)
        {
            throw new InvalidOperationException("Företaget är redan verifierat.");
        }

        var token = await _accountVerificationTokenRepository.GetLastEmailVerificationTokenAsync(companyId);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning($"Ingen token hittades för Företags-ID: {companyId}");
            return false;
        }

        await _accountVerificationTokenRepository.UpdateLastEmailVerificationTokenAsync(companyId, string.Empty);
        _logger.LogInformation($"Verifieringstoken återkallad och raderad för Företags-ID: {companyId}");
        return true;
    }

    public async Task<bool> IsVerificationTokenRevokedAsync(Guid companyId)
    {
        var token = await _accountVerificationTokenRepository.GetLastEmailVerificationTokenAsync(companyId);
        return string.IsNullOrEmpty(token);
    }

    public async Task<bool> IsVerificationTokenExpiredAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            _logger.LogWarning("Ogiltig verifieringstoken.");
            return true;
        }

        var jwtToken = handler.ReadJwtToken(token);
        if (DateTime.UtcNow > jwtToken.ValidTo)
        {
            _logger.LogWarning($"Verifieringstoken har gått ut: {token}");
            return true;
        }

        return false;
    }

    private string GenerateNewToken(Guid companyId)
    {
        // Implement your token generation logic here (JWT, GUID, etc.)
        return Guid.NewGuid().ToString(); // Placeholder
    }
}