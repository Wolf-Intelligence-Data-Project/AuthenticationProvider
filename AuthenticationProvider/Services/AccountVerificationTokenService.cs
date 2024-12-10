using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class AccountVerificationTokenService : IAccountVerificationTokenService
    {
        private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountVerificationTokenService> _logger;

        public AccountVerificationTokenService(
            IAccountVerificationTokenRepository accountVerificationTokenRepository,
            ICompanyRepository companyRepository,
            IConfiguration configuration,
            ILogger<AccountVerificationTokenService> logger)
        {
            _accountVerificationTokenRepository = accountVerificationTokenRepository;
            _companyRepository = companyRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateAccountVerificationTokenAsync(Guid companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", companyId);
                throw new ArgumentException("Invalid company ID.");
            }

            if (string.IsNullOrEmpty(company.Email))
            {
                _logger.LogWarning("Company email is null or empty: {CompanyId}", companyId);
                throw new ArgumentException("Company email is required to generate account verification token.");
            }

            // Delete existing tokens for the company
            await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);

            // Generate a new token
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentNullException("JWT settings are missing in the configuration.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, companyId.ToString()),  // companyId as NameIdentifier claim
                    new Claim(ClaimTypes.Email, company.Email),                  // Email as Email claim
                    new Claim("token_type", "AccountVerification")              // Custom claim for token type
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            // Save the token in the database
            var accountVerificationToken = new AccountVerificationToken
            {
                Token = tokenString,
                CompanyId = companyId,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await _accountVerificationTokenRepository.CreateAsync(accountVerificationToken);
            _logger.LogInformation("Account verification token created for company: {CompanyId}", companyId);

            return tokenString;
        }

        public async Task<bool> ValidateAccountVerificationTokenAsync(string token)
        {
            var storedToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);
            if (storedToken == null || storedToken.IsUsed)
            {
                _logger.LogWarning("Invalid or expired account verification token: {Token}", token);
                return false;
            }

            _logger.LogInformation("Valid account verification token found: {Token}", token);
            return true;
        }

        public async Task MarkAccountVerificationTokenAsUsedAsync(string token)
        {
            var storedToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);
            if (storedToken != null)
            {
                storedToken.IsUsed = true;
                await _accountVerificationTokenRepository.MarkAsUsedAsync(storedToken.Id);
                _logger.LogInformation("Account verification token marked as used: {TokenId}", storedToken.Id);
            }
            else
            {
                _logger.LogWarning("Attempted to mark a non-existent token as used: {Token}", token);
            }
        }

        public async Task<AccountVerificationToken> GetValidAccountVerificationTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("The provided token is invalid or empty.");
                return null;
            }

            var accountVerificationToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);

            if (accountVerificationToken == null)
            {
                _logger.LogWarning("The provided account verification token does not exist.");
                return null;
            }

            if (accountVerificationToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning($"The account verification token has expired. Token: {token}");
                return null;
            }

            if (accountVerificationToken.IsUsed)
            {
                _logger.LogWarning($"The account verification token has already been used. Token: {token}");
                return null;
            }

            return accountVerificationToken;
        }

        public async Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId)
        {
            // Calling the new RevokeAndDeleteAsync method from the repository
            await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);
            _logger.LogInformation("All account verification tokens for company {CompanyId} have been revoked and deleted.", companyId);
        }
    }
}
