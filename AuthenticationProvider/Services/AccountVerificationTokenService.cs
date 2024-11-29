using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services;

public class AccountVerificationTokenService : IAccountVerificationTokenService
{
    private readonly IAccountVerificationTokenRepository _tokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<AccountVerificationTokenService> _logger;
    private readonly IConfiguration _configuration; // Inject IConfiguration

    public AccountVerificationTokenService(
        IAccountVerificationTokenRepository tokenRepository,
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationTokenService> logger,
        IConfiguration configuration)
    {
        _tokenRepository = tokenRepository;
        _companyRepository = companyRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GenerateVerificationTokenAsync(Guid companyId)
    {
        try
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentException("Company ID is empty.");
            }

            var company = await _companyRepository.GetByGuidAsync(companyId);
            if (company != null && company.IsVerified)
            {
                throw new InvalidOperationException("The company is already verified.");
            }

            await RevokeVerificationTokenAsync(companyId); // Revoke any existing token

            var token = GenerateNewToken(companyId);
            await _tokenRepository.SaveEmailVerificationTokenAsync(companyId, token);

            _logger.LogInformation("Verification token generated successfully.");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating verification token.");
            throw;
        }
    }

    public async Task<bool> RevokeVerificationTokenAsync(Guid companyId)
    {
        try
        {
            if (companyId == Guid.Empty)
            {
                _logger.LogWarning("Attempted to revoke token for an empty Company ID.");
                return false;
            }

            var company = await _companyRepository.GetByGuidAsync(companyId);
            if (company != null && company.IsVerified)
            {
                throw new InvalidOperationException("The company is already verified.");
            }

            var token = await _tokenRepository.GetLastEmailVerificationTokenAsync(companyId);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token found to revoke.");
                return false;
            }

            await _tokenRepository.UpdateLastEmailVerificationTokenAsync(companyId, string.Empty);
            _logger.LogInformation("Verification token revoked successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while revoking verification token.");
            throw;
        }
    }

    public async Task<bool> IsVerificationTokenRevokedAsync(Guid companyId)
    {
        try
        {
            var token = await _tokenRepository.GetLastEmailVerificationTokenAsync(companyId);
            return string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if the verification token is revoked.");
            throw;
        }
    }

    public async Task<bool> IsVerificationTokenExpiredAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Invalid verification token detected.");
                return true;
            }

            var jwtToken = handler.ReadJwtToken(token);
            if (DateTime.UtcNow > jwtToken.ValidTo)
            {
                _logger.LogWarning("Verification token has expired.");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking token expiration.");
            throw;
        }
    }

    private string GenerateNewToken(Guid companyId)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] { new Claim("sub", companyId.ToString()) }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials,
                Issuer = _configuration["Jwt:Issuer"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification token.");
            throw;
        }
    }
}
