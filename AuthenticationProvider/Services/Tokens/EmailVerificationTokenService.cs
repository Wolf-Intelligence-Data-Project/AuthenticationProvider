using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthenticationProvider.Services.Tokens;

public class EmailVerificationTokenService : IEmailVerificationTokenService
{
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationTokenService> _logger;

    public EmailVerificationTokenService(
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<EmailVerificationTokenService> logger)
    {
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TokenInfoModel> GenerateEmailVerificationTokenAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation(userId.ToString());

            await _emailVerificationTokenRepository.RevokeAndDeleteAsync(userId);

            var secretKey = _configuration["JwtVerification:Key"];
            var issuer = _configuration["JwtVerification:Issuer"];
            var audience = _configuration["JwtVerification:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new ArgumentNullException("JWT-inställningar saknas i konfigurationen.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var expiration = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(30);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);

            var emailVerificationToken = new EmailVerificationTokenEntity
            {
                Id = Guid.NewGuid(),
                Token = tokenString,
                UserId = userId,
                ExpiryDate = expiration,
                IsUsed = false
            };

            await _emailVerificationTokenRepository.CreateAsync(emailVerificationToken);
            string tokenId = emailVerificationToken.Id.ToString();

            return new TokenInfoModel
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

    public async Task<bool> ValidateEmailVerificationTokenAsync(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            _logger.LogWarning("Token ID is null or empty.");
            return false;
        }

        _logger.LogInformation("Attempting to parse TokenId: {TokenId}", tokenId);

        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            _logger.LogWarning("Invalid token ID format: {TokenId}", tokenId);
            return false;
        }

        try
        {
            var emailVerificationToken = await _emailVerificationTokenRepository.GetByIdAsync(parsedTokenId);

            if (emailVerificationToken == null)
            {
                _logger.LogWarning("No email verification token found for tokenId: {TokenId}", tokenId);
                return false; 
            }

            var stockholmTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));

            if (emailVerificationToken.ExpiryDate <= stockholmTime || emailVerificationToken.IsUsed)
            {
                _logger.LogWarning("Email verification token expired or already used for tokenId: {TokenId}", tokenId);
                return false;
            }
            string token = emailVerificationToken.Token.ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token is null or empty.");
                return false;
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtVerification:Key"];
            var issuer = _configuration["JwtVerification:Issuer"];
            var audience = _configuration["JwtVerification:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT settings are missing from configuration.");
                throw new ArgumentNullException("JWT settings are missing in configuration.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (emailVerificationToken == null)
            {
                _logger.LogWarning("Email verification token not found in the database.");
                return false;
            }

            _logger.LogInformation("Email verification token found. Expiry Date: {ExpiryDate}", emailVerificationToken.ExpiryDate);

            if (emailVerificationToken.IsUsed || emailVerificationToken.ExpiryDate < stockholmTime)
            {
                _logger.LogWarning("Email verification token is either used or expired.");
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

    public async Task MarkEmailVerificationTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            var resetPasswordToken = await _emailVerificationTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !await ValidateEmailVerificationTokenAsync(tokenId.ToString()))
            {
                _logger.LogWarning("The reset password token is invalid or does not exist.");
                return;
            }

            resetPasswordToken.IsUsed = true;
            await _emailVerificationTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ett fel uppstod vid markering av token som använd.");
        }
    }

    public async Task<EmailVerificationTokenEntity> GetValidEmailVerificationTokenAsync(string tokenId)
    {
        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            throw new ArgumentException("Ogiltig token-id.");
        }

        if (!await ValidateEmailVerificationTokenAsync(tokenId))
        {
            _logger.LogError(" AAAAA!!!");
            throw new ArgumentException("Tokenet är ogiltigt eller har löpt ut.");
        }

        var emailVerificationToken = await _emailVerificationTokenRepository.GetByIdAsync(parsedTokenId);

        return emailVerificationToken;
    }
}
