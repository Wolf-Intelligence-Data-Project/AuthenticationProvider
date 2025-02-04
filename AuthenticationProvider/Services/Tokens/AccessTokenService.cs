using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models;

namespace AuthenticationProvider.Services.Tokens;

/// <summary>
/// Service for managing access tokens for users. Includes generation, validation, and revocation of JWT tokens.
/// NO REFRESH TOKEN. This website is not made for long or frequent sessions.
/// </summary>
public class AccessTokenService : IAccessTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccessTokenService> _logger;

    // In-memory storage for access tokens using a ConcurrentDictionary
    private static readonly ConcurrentDictionary<string, string> _tokenStore = new ConcurrentDictionary<string, string>();

    // In-memory blacklist for revoked tokens
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BlacklistedToken>> _blacklistedTokens = new ConcurrentDictionary<string, ConcurrentDictionary<string, BlacklistedToken>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessTokenService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration for JWT settings.</param>
    /// <param name="logger">Logger instance for logging events.</param>
    public AccessTokenService(IConfiguration configuration, ILogger<AccessTokenService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a new access token for the specified user.
    /// </summary>
    /// <param name="user">The user for whom the access token is being generated.</param>
    /// <returns>A JWT access token as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the user is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if JWT configuration is invalid.</exception>
    public string GenerateAccessToken(ApplicationUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User is not found.");

        var secretKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException("JWT configuration is missing.");
        }

        // Ensure user is verified before proceeding
        if (user.IsVerified == null)
        {
            _logger.LogWarning("The user verification status is null.");
        }

        var claims = new[] {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("companyId", user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("isVerified", user.IsVerified.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1), // Token expires in 1 hour
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Revoke the old token before issuing a new one
        RevokeAccessToken(user); // Revoke the old token

        // Store the new token in memory
        _tokenStore[user.Id] = tokenString;

        // Log the generation of the new token without revealing sensitive data
        _logger.LogInformation($"Generated new access token for user {user.UserName}.");

        return tokenString;
    }

    /// <summary>
    /// Retrieves the user ID from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to extract the user ID from.</param>
    /// <returns>The user ID associated with the token, or null if unable to parse.</returns>
    public string GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            var companyIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == "companyId");
            return companyIdClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error decoding token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates whether the specified token is valid and not blacklisted.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid, false if it is blacklisted or invalid.</returns>
    public bool IsTokenValid(string token)
    {
        try
        {
            _logger.LogInformation($"Checking if token is blacklisted.");

            // Check if the token is blacklisted
            if (_blacklistedTokens.ContainsKey(token))
            {
                _logger.LogWarning($"Token is blacklisted.");
                return false; // Token is blacklisted
            }

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
            }, out _);

            // If validation passes, the token is valid
            _logger.LogInformation("Token is valid.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Token validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Revokes the access token for the specified user by adding it to the blacklist and removing it from in-memory storage.
    /// </summary>
    /// <param name="user">The user whose access token is to be revoked.</param>
    public void RevokeAccessToken(ApplicationUser user)
    {
        try
        {
            // Check if the user has an active token
            if (_tokenStore.ContainsKey(user.Id))
            {
                var currentToken = _tokenStore[user.Id];

                // Define expiration time for the blacklisted token (30 minutes for this example)
                var expirationTime = DateTime.UtcNow.AddMinutes(30);

                // Ensure the blacklist for the user exists
                if (!_blacklistedTokens.ContainsKey(user.Id))
                {
                    _blacklistedTokens[user.Id] = new ConcurrentDictionary<string, BlacklistedToken>();
                }

                // Blacklist the old token
                _blacklistedTokens[user.Id][currentToken] = new BlacklistedToken
                {
                    Token = currentToken,
                    ExpirationTime = expirationTime
                };

                // Remove the token from in-memory storage
                _tokenStore.TryRemove(user.Id, out _);

                // Log the revocation without revealing the token
                _logger.LogInformation($"Revoked access token for user {user.UserName}.");
            }
            else
            {
                _logger.LogWarning($"No active token found for user {user.UserName}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error revoking token for user {user.UserName}: {ex.Message}");
        }
    }
}
