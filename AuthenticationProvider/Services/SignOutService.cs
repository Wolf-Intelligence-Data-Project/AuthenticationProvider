using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data;

namespace AuthenticationProvider.Services;

public class SignOutService : ISignOutService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly ILogger<SignOutService> _logger;

    public SignOutService(IAccessTokenService accessTokenService, ILogger<SignOutService> logger)
    {
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Removes the specified token to complete the sign-out process.
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
            // Check if the token is valid and remove it
            var userId = _accessTokenService.GetUserIdFromToken(token); // Retrieve the user from the token
            if (string.IsNullOrEmpty(userId) || !_accessTokenService.IsTokenValid(token))
            {
                _logger.LogInformation("Token is invalid or expired");
                return false;
            }

            // Revoke the token
            var user = new ApplicationUser { Id = userId }; // Assuming user has the Id property for matching
            _accessTokenService.RevokeAccessToken(user); // Revoke token from service

            _logger.LogInformation("Token successfully removed during sign-out.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing the token during sign-out.");
            return false;
        }
    }
}