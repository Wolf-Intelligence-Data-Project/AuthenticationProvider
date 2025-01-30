using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Interfaces.Tokens;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

// Service responsible for handling the sign-out process by removing tokens from memory cache.
public class SignOutService : ISignOutService
{
    private readonly IMemoryCache _cache;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ILogger<SignOutService> _logger;

    public SignOutService(IMemoryCache cache, IAccessTokenService accessTokenService, ILogger<SignOutService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Removes the specified token from the memory cache to complete the sign-out process.
    /// </summary>
    /// <param name="token">The token to be removed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a boolean indicating success or failure of the operation.
    /// </returns>
    public async Task<bool> SignOutAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Sign-out failed: Token is null or empty.");
            return false;
        }

        try
        {
            if (!_accessTokenService.IsTokenValid(token))
            {
                _logger.LogInformation("Token is invalid or expired: {Token}", token);
                return false;
            }

            _cache.Remove(token);

            _logger.LogInformation("Token successfully removed during sign-out: {Token}", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing the token during sign-out: {Token}", token);
            return false;
        }
    }
}
