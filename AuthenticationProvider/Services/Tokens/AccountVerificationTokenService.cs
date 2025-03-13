using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Responses.Errors;
using AuthenticationProvider.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    public async Task<string> GenerateAccountVerificationTokenAsync(Guid userId)
    {
        // Check if the user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("The user not found");
            throw new ArgumentException("Ogiltigt företag.");
        }

        // Check if the user is already verified
        if (user.IsVerified)
        {
            _logger.LogWarning("The account is already verified.");
            throw new InvalidOperationException("Företagskontot är redan verifierat.");
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("User email cannot be null.");
            throw new ArgumentException("E-post krävs för att verifiera kontot.");
        }

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

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("token_type", "AccountVerification"),
        }),
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
            Token = tokenString,
            UserId = userId,
            ExpiryDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")).AddMinutes(31),
            IsUsed = false
        };

        await _accountVerificationTokenRepository.CreateAsync(accountVerificationToken);

        return tokenString;
    }

    // Validate account verification token
    public async Task<IActionResult> ValidateAccountVerificationTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token is null or empty.");
            return new BadRequestObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtVerification:Key"];
            var issuer = _configuration["JwtVerification:Issuer"];
            var audience = _configuration["JwtVerification:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT settings are missing from configuration.");
                return new BadRequestObjectResult(ErrorResponses.TokenExpiredOrInvalid);
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

            // Check if token type is correct for account verification
            var jwtToken = validatedToken as JwtSecurityToken;
            var tokenTypeClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

            if (tokenTypeClaim != "AccountVerification")
            {
                _logger.LogWarning("Invalid token type: {TokenType}", tokenTypeClaim);
                return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
            }

            // Retrieve the token using its token string
            var tokenClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "token")?.Value;

            // Use GetByTokenAsync instead of GetByTokenIdAsync to retrieve token
            var accountVerificationToken = await _accountVerificationTokenRepository.GetByTokenAsync(tokenClaim);

            if (accountVerificationToken?.IsUsed == true)
            {
                _logger.LogWarning("Account verification token has already been used.");
                return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
            }

            return new OkObjectResult(principal); // Token is valid
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Account verification token has expired.");
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError("Invalid token: {Message}", ex.Message);
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while validating token: {Message}", ex.Message);
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
    }

    // Mark the account verification token as used
    public async Task MarkAccountVerificationTokenAsUsedAsync(string token)
    {
        // Validate the token first
        var isTokenValid = await ValidateAccountVerificationTokenAsync(token);
        if (isTokenValid is not OkObjectResult)
        {
            // If the token is invalid, return early and don't continue
            return;
        }

        var storedToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);
        if (storedToken != null)
        {
            storedToken.IsUsed = true;
            await _accountVerificationTokenRepository.MarkAsUsedAsync(storedToken.Id);
        }
        else
        {
            _logger.LogWarning("The token does not exist.");
        }
    }

    // Get valid account verification token
    public async Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string token)
    {
        // Validate the token first
        var isTokenValid = await ValidateAccountVerificationTokenAsync(token);
        if (isTokenValid is not OkObjectResult)
        {
            // If the token is invalid, return null
            return null;
        }

        var accountVerificationToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);

        if (accountVerificationToken == null)
        {
            _logger.LogWarning("The token does not exist.");
            return null;
        }

        if (accountVerificationToken.ExpiryDate < TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")))
        {
            _logger.LogWarning($"The token has expired.");
            return null;
        }

        if (accountVerificationToken.IsUsed)
        {
            _logger.LogWarning($"The token has already been used.");
            return null;
        }

        return await _accountVerificationTokenRepository.GetByTokenAsync(token);
    }
}
