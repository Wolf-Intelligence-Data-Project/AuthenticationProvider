using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AuthenticationProvider.Services.Tokens;

public class AccessTokenService : IAccessTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccessTokenService> _logger;

    // In-memory storage for access tokens using a ConcurrentDictionary
    private static readonly ConcurrentDictionary<string, string> _tokenStore = new ConcurrentDictionary<string, string>();

    // In-memory blacklist for revoked tokens
    private static readonly ConcurrentDictionary<string, bool> _blacklistedTokens = new ConcurrentDictionary<string, bool>();

    public AccessTokenService(
        IConfiguration configuration,
        ILogger<AccessTokenService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GenerateAccessToken(ApplicationUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "Användaren finns inte."); 

        var secretKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer))
        {
            throw new InvalidOperationException("Det gick inte att logga in.");
        }

        // Ensure user.IsVerified is not null or false
        if (user.IsVerified == null)
        {
            _logger.LogWarning("The user is not verified.");
        }

        var claims = new[] {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("companyId", user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("isVerified", user.IsVerified.ToString().ToLower()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Store the token in-memory with userId as the key
        _tokenStore[user.Id] = tokenString;

        return tokenString;
    }

    public void RevokeAccessToken(string token)
    {
        try
        {
            var userId = GetUserIdFromToken(token);
            if (userId != null)
            {
                // Add the token to the blacklist to prevent further usage
                _blacklistedTokens[token] = true;

                // Remove the token from in-memory storage
                _tokenStore.TryRemove(userId, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error revoking token: {ex.Message}");
        }
    }

    public string GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error decoding token: {ex.Message}");
            return null;
        }
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            if (_blacklistedTokens.ContainsKey(token))
            {
                return false;
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

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Token validation failed: {ex.Message}");
            return false;
        }
    }
}
