using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Tokens;

namespace AuthenticationProvider.Services.Tokens;

public class AccessTokenService : IAccessTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccessTokenService> _logger;
    private readonly IMemoryCache _memoryCache;

    private static readonly string TokenCacheKey = "AccessToken_"; // Prefix for the cache key
    private static readonly string BlacklistCacheKey = "Blacklist_"; // Prefix for the blacklist cache key
    private static readonly string IpCacheKey = "IpAddress_";

    public AccessTokenService(IConfiguration configuration, ILogger<AccessTokenService> logger, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    #region Main Methods

    public string GenerateAccessToken(ApplicationUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User is not found.");

        RevokeAndBlacklistAccessToken(user).Wait();

        var secretKey = _configuration["JwtAccess:Key"];
        var issuer = _configuration["JwtAccess:Issuer"];
        var audience = _configuration["JwtAccess:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT configuration is missing.");

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
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _memoryCache.Set(TokenCacheKey + user.Id, tokenString, TimeSpan.FromHours(1));

        var userIpInfo = GetUserIp();
        _memoryCache.Set(IpCacheKey + user.Id, userIpInfo.IpAddress, TimeSpan.FromHours(1)); // Store IP for 1 hour

        _httpContextAccessor.HttpContext?.Response?.Cookies.Append("AccessToken", tokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(1)
        });

        _logger.LogInformation($"Generated new access token for user {user.UserName}.");

        return tokenString;
    }

    public (bool isAuthenticated, bool isAccountVerified) ValidateAccessToken(string token = null)
    {
        try
        {
            token ??= _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"] ?? string.Empty;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided.");
                return (false, false);
            }

            var isTokenValid = CheckBlacklist(token);
            if (!isTokenValid)
            {
                _logger.LogWarning("Token is expired, invalid, or blacklisted.");
                return (false, false);
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _configuration["JwtAccess:Issuer"],
                ValidAudience = _configuration["JwtAccess:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtAccess:Key"]))
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);

            var isAccountVerified = principal.Claims.FirstOrDefault(c => c.Type == "isVerified")?.Value == "true";
            if (!isAccountVerified)
            {
                _logger.LogWarning("Token user is not verified.");
                return (false, false);
            }

            var storedIp = _memoryCache.Get<string>(IpCacheKey + principal.Identity.Name);
            var currentIp = GetUserIp().IpAddress;

            if (storedIp != "Unknown IP" && storedIp != currentIp)
            {
                _logger.LogWarning("IP address mismatch. Token validation failed.");
                return (false, false);
            }

            _logger.LogInformation("Token is valid and user is verified.");
            return (true, true);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return (false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error during token validation: {ex.Message}");
            return (false, false);
        }
    }

    public async Task RevokeAndBlacklistAccessToken(ApplicationUser user)
    {
        if (user == null)
        {
            _logger.LogWarning("Attempted to revoke token for a null user.");
            return;
        }

        try
        {
            var tokenKey = TokenCacheKey + user.Id;

            // Check if there's an active token in _memoryCache for this user
            if (_memoryCache.TryGetValue(tokenKey, out var currentToken))
            {
                // Calculate expiration time in Stockholm time zone, 75 minutes from now.
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
                var expirationTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(75), timeZone);

                // Add the token to the blacklist with a cache expiration of 75 minutes.
                _memoryCache.Set(BlacklistCacheKey + currentToken.ToString(), new BlacklistedToken
                {
                    Token = currentToken.ToString(),
                    ExpirationTime = expirationTime
                }, TimeSpan.FromMinutes(75));

                // Remove the token from the active token cache
                _memoryCache.Remove(tokenKey);

                // Remove the IP address from cache as well
                _memoryCache.Remove(IpCacheKey + user.Id);

                _logger.LogInformation($"Token for user {user.UserName} revoked and added to blacklist.");

                // Clear the cookie to invalidate the session
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    context.Response.Cookies.Delete("AccessToken");
                    _logger.LogInformation($"Access token cleared for user {user.UserName}.");
                }

            }

            else
            {
                _logger.LogWarning($"No active token found for user {user.UserName}. Token not revoked.");
            }

        }

        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revoking token for user {user.UserName}.");
        }
    }

    #endregion

    #region Helper Methods

    // Helper method to get the user's IP and User-Agent
    public UserIpInfo GetUserIp()
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown User-Agent";

        return new UserIpInfo
        {
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

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

    private bool CheckBlacklist(string token)
    {
        if (_memoryCache.TryGetValue(BlacklistCacheKey + token, out BlacklistedToken blacklistedToken))
        {
            if (blacklistedToken.ExpirationTime > DateTime.UtcNow)
            {
                _logger.LogWarning("Token is blacklisted.");
                return false;  // Token is blacklisted
            }
            _memoryCache.Remove(BlacklistCacheKey + token);
        }
        return true;  // Token is valid
    }

    #endregion
}
