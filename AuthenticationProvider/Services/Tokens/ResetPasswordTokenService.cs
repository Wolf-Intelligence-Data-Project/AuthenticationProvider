using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Interfaces.Services.Tokens;

namespace AuthenticationProvider.Services.Tokens;

public class ResetPasswordTokenService : IResetPasswordTokenService
{
    private readonly IResetPasswordTokenRepository _resetPasswordTokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResetPasswordTokenService> _logger;
    private readonly PasswordHasher<CompanyEntity> _passwordHasher;

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
        _passwordHasher = new PasswordHasher<CompanyEntity>();
    }

    public async Task<string> CreateResetPasswordTokenAsync(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email is null or empty.");
                throw new ArgumentException("E-postadress är obligatorisk.");
            }

            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                throw new ArgumentException("Inget företag hittades med den angivna e-postadressen.");
            }

            // Delete previous token if any exists
            await _resetPasswordTokenRepository.DeleteAsync(company.Id);

            var secretKey = _configuration["JwtResetPassword:Key"];
            var issuer = _configuration["JwtResetPassword:Issuer"];
            var audience = _configuration["JwtResetPassword:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentNullException("JWT-inställningar saknas i konfigurationen.");
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
                new Claim("token_type", "ResetPassword"),
                new Claim(JwtRegisteredClaimNames.Aud, audience)
            }),
                Expires = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            var resetPasswordToken = new ResetPasswordTokenEntity
            {
                Token = tokenString,
                CompanyId = company.Id,
                ExpiryDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(15),
                IsUsed = false
            };

            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);

            return tokenString;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            throw new InvalidOperationException("Ett oväntat fel uppstod, försök igen senare.");
        }
    }

    public async Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token)
    {
        if (!await ValidateResetPasswordTokenAsync(token))
        {
            throw new ArgumentException("Tokenet är ogiltigt eller har löpt ut.");
        }

        return await _resetPasswordTokenRepository.GetByTokenAsync(token);
    }

    public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !await ValidateResetPasswordTokenAsync(resetPasswordToken.Token))
            {
                _logger.LogWarning("The reset password token is invalid or does not exist.");
                return;
            }

            resetPasswordToken.IsUsed = true;
            await _resetPasswordTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ett fel uppstod vid markering av token som använd.");
        }
    }

    public async Task<bool> ValidateResetPasswordTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token is null or empty.");
            return false;
        }

        try
        {
            var secretKey = _configuration["JwtResetPassword:Key"];
            var issuer = _configuration["JwtResetPassword:Issuer"];
            var audience = _configuration["JwtResetPassword:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new ArgumentNullException("JWT settings are missing in configuration.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null || !resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")))
            {
                _logger.LogWarning("Reset password token is invalid, used, or expired.");
                return false;
            }

            return true;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating the reset password token.");
            return false;
        }
    }
}
