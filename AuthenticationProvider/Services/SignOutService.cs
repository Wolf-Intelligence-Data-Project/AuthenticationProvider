using AuthenticationProvider.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

/// <summary>
/// Service responsible for handling the sign-out process by removing tokens from memory cache.
/// </summary>
public class SignOutService : ISignOutService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SignOutService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignOutService"/> class.
    /// </summary>
    /// <param name="cache">An instance of <see cref="IMemoryCache"/> for managing cached tokens.</param>
    /// <param name="logger">An instance of <see cref="ILogger{SignOutService}"/> for logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided arguments are null.</exception>
    public SignOutService(IMemoryCache cache, ILogger<SignOutService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
            return false; // Invalid token input, sign-out cannot proceed
        }

        try
        {
            // Attempt to remove the token from memory cache
            _cache.Remove(token);

            _logger.LogInformation("Token successfully removed during sign-out: {Token}", token);
            return true; // Operation successful
        }
        catch (Exception ex)
        {
            // Log and handle unexpected errors during token removal
            _logger.LogError(ex, "An error occurred while removing the token during sign-out: {Token}", token);
            return false; // Operation failed
        }
    }
}
