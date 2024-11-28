using AuthenticationProvider.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace AuthenticationProvider.Services;

public class LoginSessionTokenService : ILoginSessionTokenService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<LoginSessionTokenService> _logger;

    // Updated constructor - removed the circular dependency
    public LoginSessionTokenService(ICompanyRepository companyRepository, ILogger<LoginSessionTokenService> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<string> GenerateLoginSessionTokenAsync(string email)
    {
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            throw new ArgumentException("Företag hittades inte."); // Swedish: Company not found
        }

        // Check if the company is verified before generating the login session token
        if (!company.IsVerified)
        {
            throw new InvalidOperationException("E-posten är inte verifierad. Kan inte generera inloggningssessionstoken."); // Swedish: Email is not verified. Cannot generate session token.
        }

        // Implement the logic for generating a session token directly here
        var token = "generated-session-token"; // Replace this with actual token generation logic

        // Update the company with the new login session token
        company.LastLoginSessionToken = token;
        await _companyRepository.UpdateAsync(company);

        _logger.LogInformation($"Inloggningssessionstoken genererad för företag: {company.CompanyName}, Email: {email}"); // Swedish: Login session token generated
        return token;
    }

    public async Task<bool> InvalidateLoginSessionTokenAsync(string email)
    {
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            return false;
        }

        company.LastLoginSessionToken = string.Empty;
        await _companyRepository.UpdateAsync(company);

        _logger.LogInformation($"Inloggningssessionstoken ogiltigförklarad för företag: {company.CompanyName}, Email: {email}"); // Swedish: Login session token invalidated
        return true;
    }

    public async Task<bool> IsLoginSessionTokenExpiredAsync(string email)
    {
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null || string.IsNullOrEmpty(company.LastLoginSessionToken))
        {
            return true; // No token found or company not found, so considered expired
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(company.LastLoginSessionToken))
        {
            _logger.LogWarning($"Ogiltig inloggningssessionstoken: {company.LastLoginSessionToken}"); // Swedish: Invalid login session token
            return true; // Token is invalid
        }

        var jwtToken = handler.ReadJwtToken(company.LastLoginSessionToken);
        if (DateTime.UtcNow > jwtToken.ValidTo)
        {
            _logger.LogWarning($"Inloggningssessionstoken har gått ut: {company.LastLoginSessionToken}"); // Swedish: Login session token has expired
            return true; // Token expired
        }

        return false; // Token is valid
    }

    public async Task<bool> RevokeLoginSessionTokenAsync(string email)
    {
        // Step 1: Retrieve the company by email
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null || string.IsNullOrEmpty(company.LastLoginSessionToken))
        {
            return false; // No token to revoke
        }

        // Step 2: Revoke the login session token (clear it)
        company.LastLoginSessionToken = string.Empty;
        await _companyRepository.UpdateAsync(company);

        // Log the successful revocation of the login session token
        _logger.LogInformation($"Inloggningssessionstoken återkallad för företag: {company.CompanyName}, Email: {email}"); // Swedish: Login session token revoked
        return true;
    }
}
