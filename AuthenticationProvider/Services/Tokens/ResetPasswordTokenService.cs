using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthenticationProvider.Services.Tokens
{
    public class ResetPasswordTokenService : IResetPasswordTokenService
    {
        private readonly IResetPasswordTokenRepository _resetPasswordTokenRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResetPasswordTokenService> _logger;
        private readonly PasswordHasher<CompanyEntity> _passwordHasher; // Add PasswordHasher

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
            _passwordHasher = new PasswordHasher<CompanyEntity>(); // Initialize PasswordHasher
        }

        public async Task<string> CreateResetPasswordTokenAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email is null or empty.");
                throw new ArgumentException("Email is required to generate reset password token.");
            }

            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                throw new ArgumentException("No company found with the provided email.");
            }

            if (string.IsNullOrEmpty(company.Email))
            {
                _logger.LogWarning("Company email is null or empty: {CompanyId}", company.Id);
                throw new ArgumentException("Company email is required to generate reset password token.");
            }

            // Delete existing tokens for the company
            await _resetPasswordTokenRepository.DeleteAsync(company.Id);

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
                    new Claim(ClaimTypes.NameIdentifier, company.Id.ToString()),
                    new Claim(ClaimTypes.Email, company.Email),
                    new Claim("token_type", "ResetPassword")
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
                CompanyId = company.Id,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);
            _logger.LogInformation("Reset password token created for company: {CompanyId}", company.Id);

            return tokenString;
        }

        public async Task<bool> ResetCompanyPasswordAsync(string email, string newPassword)
        {
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return false;
            }

            // Hash the new password using PasswordHasher
            string hashedPassword = _passwordHasher.HashPassword(null, newPassword);

            company.PasswordHash = hashedPassword;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation("Password reset successfully for company: {CompanyId}", company.Id);
            return true;
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
        public async Task<string> GetEmailFromTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Provided token is null or empty.");
                throw new ArgumentException("Token is required.");
            }

            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || resetPasswordToken.IsUsed)
            {
                _logger.LogWarning("Invalid or used reset password token: {Token}", token);
                return null;
            }

            // Ensure the token is still valid
            if (resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("The reset password token has expired. Token: {Token}", token);
                return null;
            }

            // Fetch the company associated with the token
            var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
            if (company == null)
            {
                _logger.LogWarning("No company found for the token: {Token}", token);
                return null;
            }

            return company.Email;
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
