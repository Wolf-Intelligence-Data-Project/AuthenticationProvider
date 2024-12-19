using AuthenticationProvider.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;

namespace AuthenticationProvider.Services.Tokens
{
    public class ResetPasswordTokenService : IResetPasswordTokenService
    {
        private readonly IResetPasswordTokenRepository _resetPasswordTokenRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResetPasswordTokenService> _logger;

        public ResetPasswordTokenService(
            IResetPasswordTokenRepository resetPasswordTokenRepository,
            ICompanyRepository companyRepository,
            IConfiguration configuration,
            ILogger<ResetPasswordTokenService> logger)
        {
            _resetPasswordTokenRepository = resetPasswordTokenRepository;
            _companyRepository = companyRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateResetPasswordTokenAsync(Guid companyId)
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
                throw new ArgumentException("Company email is required to generate reset password token.");
            }

            // Delete existing tokens for the company
            await _resetPasswordTokenRepository.DeleteAsync(companyId);

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

            // Add both the email ('sub') and token type ('token_type') claims
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, companyId.ToString()),  // companyId as NameIdentifier claim
            new Claim(ClaimTypes.Email, company.Email),                 // Email as Email claim
            new Claim("token_type", "ResetPassword")             // Custom claim for token type
            }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            // Save the token in the database
            var resetPasswordToken = new ResetPasswordTokenEntity
            {
                Token = tokenString,
                CompanyId = companyId,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);
            _logger.LogInformation("Reset password token created for company: {CompanyId}", companyId);

            return tokenString;
        }


        public async Task<bool> ValidateResetPasswordTokenAsync(string token)
        {
            var storedToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (storedToken == null || storedToken.IsUsed)
            {
                _logger.LogWarning("Invalid or expired reset password token: {Token}", token);
                return false;
            }

            _logger.LogInformation("Valid reset password token found: {Token}", token);
            return true;
        }

        public async Task MarkResetPasswordTokenAsUsedAsync(string token)
        {
            var storedToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (storedToken != null)
            {
                storedToken.IsUsed = true;
                await _resetPasswordTokenRepository.MarkAsUsedAsync(storedToken.Id);
                _logger.LogInformation("Reset password token marked as used: {TokenId}", storedToken.Id);
            }
            else
            {
                _logger.LogWarning("Attempted to mark a non-existent token as used: {Token}", token);
            }
        }

        public async Task<bool> ResetCompanyPasswordAsync(string email, string newPassword)
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return false;
            }

            company.PasswordHash = HashPassword(newPassword);
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Password reset successfully for company: {CompanyId}", company.Id);
            return true;
        }

        private string HashPassword(string password)
        {
            // Temporarily returning normal password (development)
            return password;
        }

        public async Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("The provided token is invalid or empty.");
                return null;
            }

            // Fetch the token from the repository
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);

            if (resetPasswordToken == null)
            {
                _logger.LogWarning("The provided reset password token does not exist.");
                return null;
            }

            // Check if the token has expired
            if (resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning($"The reset password token has expired. Token: {token}");
                return null;
            }

            // Check if the token has already been used
            if (resetPasswordToken.IsUsed)
            {
                _logger.LogWarning($"The reset password token has already been used. Token: {token}");
                return null;
            }

            // Return the valid token
            return resetPasswordToken;
        }

        public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken != null)
            {
                resetPasswordToken.IsUsed = true;
                await _resetPasswordTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
                _logger.LogInformation("Reset password token marked as used: {TokenId}", resetPasswordToken.Id);
            }
            else
            {
                _logger.LogWarning("Attempted to mark a non-existent token as used: {TokenId}", tokenId);
            }
        }

        public async Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId)
        {
            await _resetPasswordTokenRepository.DeleteAsync(companyId);
            _logger.LogInformation("All reset password tokens for company {CompanyId} have been deleted.", companyId);
        }
    }
}
