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
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResetPasswordTokenService> _logger;

    public ResetPasswordTokenService(
        IResetPasswordTokenRepository resetPasswordTokenRepository,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<ResetPasswordTokenService> logger)
    {
        _resetPasswordTokenRepository = resetPasswordTokenRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<object> GenerateResetPasswordTokenAsync(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email is null or empty.");
                throw new ArgumentException("E-postadress är obligatorisk.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found with email: {Email}", email);
                throw new ArgumentException("Inget företag hittades med den angivna e-postadressen.");
            }

            // Delete previous token if any exists
            await _resetPasswordTokenRepository.DeleteAsync(user.UserId);

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
                new Claim("token_type", "ResetPassword"),
            }),
                Expires = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(jwtToken);

            var resetPasswordToken = new ResetPasswordTokenEntity
            {
                Id = Guid.NewGuid(),
                Token = tokenString,
                UserId = user.UserId,
                ExpiryDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(31),
                IsUsed = false
            };

            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);
            string tokenId = resetPasswordToken.ToString()!;

            // Returning both token string and token id, because it will validate the token before sending but it will send only token id
            return new { TokenId = tokenId, TokenString = tokenString };
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
                ClockSkew = TimeSpan.Zero  // No tolerance for expired tokens
            };

            // Validate the token
            var validatedToken = tokenHandler.ValidateToken(token, validationParameters, out var validated);
            _logger.LogInformation("Token validated successfully.");

            // Now check the token from the database (if it's not expired or used)
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByTokenAsync(token);
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Reset password token not found in the database.");
                return false;
            }

            _logger.LogInformation("Reset password token found. Expiry Date: {ExpiryDate}", resetPasswordToken.ExpiryDate);

            // Check if the token is used or expired
            if (resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Reset password token is either used or expired.");
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
