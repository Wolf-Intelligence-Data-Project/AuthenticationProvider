using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class AccountVerificationTokenService : IAccountVerificationTokenService
    {
        private readonly IAccountVerificationTokenRepository _tokenRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountVerificationTokenService> _logger;

        public AccountVerificationTokenService(
            IAccountVerificationTokenRepository tokenRepository,
            ICompanyRepository companyRepository,
            IConfiguration configuration,
            ILogger<AccountVerificationTokenService> logger)
        {
            _tokenRepository = tokenRepository;
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

            await _tokenRepository.DeleteAsync(companyId);

            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
            {
                _logger.LogError("JWT settings are missing in the configuration.");
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
            new Claim(ClaimTypes.Email, company.Email),                 // Email as Email claim
            new Claim("token_type", "AccountVerification")             // Custom claim for token type
            }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            var accountVerificationToken = new AccountVerificationToken
            {
                Token = tokenString,
                CompanyId = companyId,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await _tokenRepository.CreateAsync(accountVerificationToken);
            _logger.LogInformation("Account verification token created for company: {CompanyId}", companyId);

            return tokenString;
        }


        public async Task<bool> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Validate the token and get claims
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var companyIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tokenTypeClaim = claimsPrincipal.FindFirst("token_type")?.Value;

                if (string.IsNullOrEmpty(companyIdClaim) || tokenTypeClaim != "AccountVerification")
                {
                    _logger.LogWarning("Invalid token claims: {Token}", token);
                    return false;
                }

                // Get the token from the database and verify expiration and usage status
                var storedToken = await _tokenRepository.GetByTokenAsync(token);
                if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow || storedToken.IsUsed)
                {
                    _logger.LogWarning("Invalid or expired token: {Token}", token);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed.");
                return false;
            }
        }

        public async Task MarkTokenAsUsedAsync(string token)
        {
            var storedToken = await _tokenRepository.GetByTokenAsync(token);
            if (storedToken != null)
            {
                await _tokenRepository.MarkAsUsedAsync(storedToken.Id);
                _logger.LogInformation("Token marked as used: {TokenId}", storedToken.Id);
            }
            else
            {
                _logger.LogWarning("Attempt to mark non-existent token as used: {Token}", token);
            }
        }

        public Task<bool> SendVerificationEmailAsync(string token, string email)
        {
            throw new NotImplementedException();
        }
    }
}
