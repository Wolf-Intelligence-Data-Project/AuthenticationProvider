using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthenticationProvider.Services;

public class LoginSessionTokenService : ILoginSessionTokenService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<LoginSessionTokenService> _logger;

    public LoginSessionTokenService(ICompanyRepository companyRepository, ILogger<LoginSessionTokenService> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<string> GenerateLoginSessionTokenAsync(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found for email verification attempt.");
                throw new ArgumentException("Företag hittades inte.");
            }

            if (!company.IsVerified)
            {
                _logger.LogWarning("Email not verified for company verification attempt.");
                throw new InvalidOperationException("E-posten är inte verifierad.");
            }

            var token = "generated-session-token"; // Replace with actual token generation logic

            company.LastLoginSessionToken = token;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Login session token generated successfully.");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating login session token.");
            throw;
        }
    }

    public async Task<bool> InvalidateLoginSessionTokenAsync(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found for invalidating login session token.");
                return false;
            }

            company.LastLoginSessionToken = string.Empty;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Login session token invalidated successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating login session token.");
            return false;
        }
    }

    public async Task<bool> IsLoginSessionTokenExpiredAsync(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null || string.IsNullOrEmpty(company.LastLoginSessionToken))
            {
                return true;
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(company.LastLoginSessionToken))
            {
                _logger.LogWarning("Invalid login session token detected.");
                return true;
            }

            var jwtToken = handler.ReadJwtToken(company.LastLoginSessionToken);
            if (DateTime.UtcNow > jwtToken.ValidTo)
            {
                _logger.LogWarning("Login session token has expired.");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking expiration for login session token.");
            return true;
        }
    }

    public async Task<bool> RevokeLoginSessionTokenAsync(string email)
    {
        try
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null || string.IsNullOrEmpty(company.LastLoginSessionToken))
            {
                _logger.LogWarning("No login session token to revoke for the company.");
                return false;
            }

            company.LastLoginSessionToken = string.Empty;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Login session token revoked successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking login session token.");
            return false;
        }
    }
}
