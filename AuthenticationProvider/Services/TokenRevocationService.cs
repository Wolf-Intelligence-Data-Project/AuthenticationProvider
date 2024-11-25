using AuthenticationProvider.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace AuthenticationProvider.Services;

public class TokenRevocationService : ITokenRevocationService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(1); // Token expiration time

    public TokenRevocationService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<bool> RevokeTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(false);

        // Add token to blacklist with expiration
        _cache.Set(token, true, _tokenLifetime);
        return Task.FromResult(true);
    }

    public bool IsTokenRevoked(string token)
    {
        // Check if the token exists in the blacklist
        return _cache.TryGetValue(token, out _);
    }
}