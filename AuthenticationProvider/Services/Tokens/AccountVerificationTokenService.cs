using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthenticationProvider.Services.Tokens;

public class AccountVerificationTokenService : IAccountVerificationTokenService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountVerificationTokenService> _logger;

    public AccountVerificationTokenService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AccountVerificationTokenService> logger)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TokenInfo> GenerateAccountVerificationTokenAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation(userId.ToString());

            // Revoke and delete any existing verification tokens
            await _accountVerificationTokenRepository.RevokeAndDeleteAsync(userId);

            // Generate a new JWT token
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

            // Save the new token in the database
            var accountVerificationToken = new AccountVerificationTokenEntity
            {
                Id = Guid.NewGuid(),
                Token = tokenString,
                UserId = userId,
                ExpiryDate = expiration,
                IsUsed = false
            };

            await _accountVerificationTokenRepository.CreateAsync(accountVerificationToken);
            string tokenId = accountVerificationToken.Id.ToString();
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

    // Validate account verification token
    public async Task<bool> ValidateAccountVerificationTokenAsync(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            _logger.LogWarning("Token ID is null or empty.");
            return false;
        }

        _logger.LogInformation("Attempting to parse TokenId: {TokenId}", tokenId);

        // Try to parse the tokenId as a GUID
        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            _logger.LogWarning("Invalid token ID format: {TokenId}", tokenId);
            return false;
        }

        try
        {
            var accountVerificationToken = await _accountVerificationTokenRepository.GetByIdAsync(parsedTokenId);

            // Check if the token exists
            if (accountVerificationToken == null)
            {
                _logger.LogWarning("No account verification token found for tokenId: {TokenId}", tokenId);
                return false; // Token not found
            }

            // Perform additional checks: expiry and usage status
            var stockholmTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));

            // Check if the token has expired or if it is already used
            if (accountVerificationToken.ExpiryDate <= stockholmTime || accountVerificationToken.IsUsed)
            {
                _logger.LogWarning("Account verification token expired or already used for tokenId: {TokenId}", tokenId);
                return false; // Invalid token
            }
            string token = accountVerificationToken.Token.ToString();
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

            // Validate the token and extract claims
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Now check the token taken from the database (if it's not expired or used)
            if (accountVerificationToken == null)
            {
                _logger.LogWarning("Account verification token not found in the database.");
                return false;
            }

            _logger.LogInformation("Account verification token found. Expiry Date: {ExpiryDate}", accountVerificationToken.ExpiryDate);

            // Check if the token is used or expired
            if (accountVerificationToken.IsUsed || accountVerificationToken.ExpiryDate < stockholmTime)
            {
                _logger.LogWarning("Account verification token is either used or expired.");
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

    // Mark the account verification token as used
    public async Task MarkAccountVerificationTokenAsUsedAsync(Guid tokenId)
    {
        try
        {
            var resetPasswordToken = await _accountVerificationTokenRepository.GetByIdAsync(tokenId);
            if (resetPasswordToken == null || !await ValidateAccountVerificationTokenAsync(tokenId.ToString()))
            {
                _logger.LogWarning("The reset password token is invalid or does not exist.");
                return;
            }

            resetPasswordToken.IsUsed = true;
            await _accountVerificationTokenRepository.MarkAsUsedAsync(resetPasswordToken.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ett fel uppstod vid markering av token som använd.");
        }
    }

    // Get valid account verification token
    public async Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string tokenId)
    {
        if (!Guid.TryParse(tokenId, out Guid parsedTokenId))
        {
            throw new ArgumentException("Ogiltig token-id.");
        }

        if (!await ValidateAccountVerificationTokenAsync(tokenId))
        {
            _logger.LogError(" AAAAA!!!");
            throw new ArgumentException("Tokenet är ogiltigt eller har löpt ut.");
        }

        var accountVerificationToken = await _accountVerificationTokenRepository.GetByIdAsync(parsedTokenId);

        return accountVerificationToken;
    }
}
