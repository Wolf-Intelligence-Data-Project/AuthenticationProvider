using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models;

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

    public async Task<TokenInfo> GenerateResetPasswordTokenAsync(string email)
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
            var expiration = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(30);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = expiration,
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
                ExpiryDate = expiration,
                IsUsed = false
            };

            await _resetPasswordTokenRepository.CreateAsync(resetPasswordToken);
            string tokenId = resetPasswordToken.Id.ToString();

            // Return a structured object instead of just a tokenId
            return new TokenInfo
            {
                TokenId = tokenId,
                TokenString = tokenString
            };
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

    public async Task<bool> ValidateResetPasswordTokenAsync(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            _logger.LogWarning("Token ID is null or empty.");
            return false;
        }

        // Clean up tokenId by trimming any extra spaces
        tokenId = tokenId.Trim();

        _logger.LogInformation("Attempting to parse TokenId: {TokenId}", tokenId);

        // Try to parse the tokenId as a GUID
        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            _logger.LogWarning("Invalid token ID format: {TokenId}", tokenId);
            return false;
        }

        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(parsedTokenId);

            // Check if the token exists
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("No reset password token found for tokenId: {TokenId}", tokenId);
                return false; // Token not found
            }

            // Perform additional checks: expiry and usage status
            var stockholmTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));

            // Check if the token has expired or if it is already used
            if (resetPasswordToken.ExpiryDate <= stockholmTime || resetPasswordToken.IsUsed)
            {
                _logger.LogWarning("Reset password token expired or already used for tokenId: {TokenId}", tokenId);
                return false; // Invalid token
            }
            string token = resetPasswordToken.Token.ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token is null or empty.");
                return false;
            }

            var secretKey = _configuration["JwtResetPassword:Key"];
            var issuer = _configuration["JwtResetPassword:Issuer"];
            var audience = _configuration["JwtResetPassword:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT settings are missing from configuration.");
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

            // Now check the token taken from the database (if it's not expired or used)
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Reset password token not found in the database.");
                return false;
            }

            _logger.LogInformation("Reset password token found. Expiry Date: {ExpiryDate}", resetPasswordToken.ExpiryDate);

            // Check if the token is used or expired
            if (resetPasswordToken.IsUsed || resetPasswordToken.ExpiryDate < stockholmTime)
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

    public async Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string tokenId)
    {
        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            throw new ArgumentException("Ogiltig token-id.");
        }

        var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(parsedTokenId);

        if (resetPasswordToken == null || !await ValidateResetPasswordTokenAsync(tokenId))
        {
            throw new ArgumentException("Tokenet är ogiltigt eller har löpt ut.");
        }

        return resetPasswordToken;
    }

    public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            var resetPasswordToken = await _resetPasswordTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !await ValidateResetPasswordTokenAsync(tokenId.ToString()))
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
}
