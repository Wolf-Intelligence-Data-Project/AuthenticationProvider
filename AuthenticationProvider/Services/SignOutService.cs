using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class SignOutService : ISignOutService
    {
        private readonly IMemoryCache _cache;

        public SignOutService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<bool> SignOutAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false; // Token is required for sign-out
            }

            try
            {
                // Check if the token is already blacklisted
                if (_cache.TryGetValue(token, out bool isBlacklisted) && isBlacklisted)
                {
                    return true; // Token is already blacklisted, sign-out is already done
                }

                // Add the token to a blacklist cache with an expiration equal to its remaining validity period
                var expiration = GetTokenExpiration(token);
                if (expiration.HasValue)
                {
                    _cache.Set(token, true, expiration.Value - DateTime.UtcNow);
                }
                else
                {
                    // If token expiration cannot be determined, use a default short lifespan
                    _cache.Set(token, true, TimeSpan.FromHours(1));
                }

                return true; // Successfully blacklisted
            }
            catch
            {
                return false; // An error occurred while signing out
            }
        }

        private DateTime? GetTokenExpiration(string token)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return null; // Invalid token format
            }

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo; // Return the expiration time (UTC)
        }
    }
}
