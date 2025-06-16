using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Tokens;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Services.Tokens;

public class AccessTokenService : IAccessTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccessTokenService> _logger;
    private readonly IMemoryCache _memoryCache;

    private static readonly string TokenCacheKey = "AccessToken_";
    private static readonly string BlacklistCacheKey = "Blacklist_"; 
    private static readonly string IpCacheKey = "IpAddress_";

    private readonly List<string> _cacheKeys = new List<string>();

    public AccessTokenService(IConfiguration configuration, IUserRepository userRepository, ILogger<AccessTokenService> logger, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _userRepository = userRepository;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    #region Main Methods

    public async Task<string> GenerateAccessToken(UserEntity user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User is not found.");

        var userIpInfo = GetUserIp();
        var ipCacheKey = IpCacheKey + user.UserId;

        if (_memoryCache.TryGetValue(ipCacheKey, out var existingIpAddress))
        {
            if (existingIpAddress.ToString() == userIpInfo.IpAddress)
            {
                _logger.LogInformation($"User {user.FullName} already has an active token for IP {userIpInfo.IpAddress}, revoking the old one.");
                await RevokeAndBlacklistAccessToken(user.UserId.ToString()); 
            }
        }

        var userEntity = await _userRepository.GetByIdAsync(user.UserId);
        if (userEntity.IsVerified != true)
        {
            _logger.LogInformation($"User {user.FullName} is not verified.");
            return null;
        }

        var secretKey = _configuration["JwtAccess:Key"];
        var issuer = _configuration["JwtAccess:Issuer"];
        var audience = _configuration["JwtAccess:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT configuration is missing.");

        var claims = new[] {
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim("userId", user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("isVerified", user.IsVerified.ToString().ToLower())
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _memoryCache.Set(TokenCacheKey + user.UserId, tokenString, TimeSpan.FromHours(1));
        _cacheKeys.Add(TokenCacheKey + user.UserId); 

        _memoryCache.Set(ipCacheKey, userIpInfo.IpAddress, TimeSpan.FromHours(1));
        _cacheKeys.Add(ipCacheKey); 

        _httpContextAccessor.HttpContext?.Response?.Cookies.Append("AccessToken", tokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.Now.AddHours(1)
        });

        _logger.LogInformation($"Generated new access token for user {user.FullName}.");

        return tokenString;
    }

    public (bool isAuthenticated, bool isEmailVerified) ValidateAccessToken(string token)
    {
        try
        {
            token ??= _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"] ?? string.Empty;


            if (string.IsNullOrEmpty(token) || !CheckBlacklist(token))
            {
                _logger.LogWarning("Token is either missing or blacklisted.");
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

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserId not found in token claims.");
                return (false, false);
            }

            // IP validation
            var storedIp = _memoryCache.Get<string>(IpCacheKey + userId);
            var currentIp = GetUserIp().IpAddress;
            bool isAuthenticated = storedIp == null || storedIp == currentIp;

            _logger.LogInformation($"Token validation successful for User ID: {userId}. IP check: {isAuthenticated}");

            var isEmailVerified = principal.Claims.FirstOrDefault(c => c.Type == "isVerified")?.Value == "true";
            if (!isEmailVerified)
            {
                _logger.LogWarning("Token user is not verified.");
            }

            // Return both isAuthenticated and isEmailVerified statuses.
            return (isAuthenticated, isEmailVerified);

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

    public async Task RevokeAndBlacklistAccessToken(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Attempted to revoke token for a null or empty user ID.");
            return;
        }

        try
        {
            var tokenKey = TokenCacheKey + userId;
            if (_memoryCache.TryGetValue(tokenKey, out var currentToken))
            {
                // Blacklist the old token
                _memoryCache.Set(BlacklistCacheKey + currentToken.ToString(), new BlacklistedToken
                {
                    Token = currentToken.ToString(),
                    ExpirationTime = DateTime.Now.AddMinutes(75)
                }, TimeSpan.FromMinutes(75));

                // Remove the old token from cache
                _memoryCache.Remove(tokenKey);
                _memoryCache.Remove(IpCacheKey + userId);

                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    context.Response.Cookies.Delete("AccessToken");
                    _logger.LogInformation($"Access token cleared for user {userId}.");
                }
            }
            else
            {
                _logger.LogWarning($"No active token found for user {userId}. Token not revoked.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revoking token for user {userId}.");
        }
    }

    #endregion

    #region Helper Methods

    private UserIpInfo GetUserIp()
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        if (ipAddress == "::1" || ipAddress == "127.0.0.1")
        {
            ipAddress = "127.0.0.1";
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            // Generate a GUID as fallback for missing IP
            ipAddress = Guid.NewGuid().ToString();
            _logger.LogInformation($"Generated GUID as fallback: {ipAddress}");
        }

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

            var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == "userId");
            return userIdClaim?.Value;
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
            if (blacklistedToken.ExpirationTime > TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")))
            {
                _logger.LogWarning("Token is blacklisted.");
                return false; 
            }
            _memoryCache.Remove(BlacklistCacheKey + token);
        }
        return true;
    }

        #region Utilities 
        public void CleanUpExpiredTokens()
        {
            var currentTime = DateTime.Now;
            foreach (var tokenKey in _cacheKeys) 
            {
                if (IsTokenExpired(tokenKey, currentTime))
                {
                    _memoryCache.Remove(tokenKey);
                    _memoryCache.Remove(IpCacheKey + tokenKey);
                    _logger.LogInformation($"Expired token and IP/GUID removed for token: {tokenKey}");
                }
            }
        }

        private bool IsTokenExpired(string tokenKey, DateTime currentTime)
        {
            var token = _memoryCache.Get<string>(tokenKey);
            return token != null && IsJwtExpired(token, currentTime);
        }

        private bool IsJwtExpired(string token, DateTime currentTime)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                return false;
            }

            var expiration = jwtToken?.Payload?.Expiration?.ToString();
            if (DateTime.TryParse(expiration, out var expiryTime))
            {
                return currentTime > expiryTime;
            }

            return false;
        }
        #endregion

    #endregion
}