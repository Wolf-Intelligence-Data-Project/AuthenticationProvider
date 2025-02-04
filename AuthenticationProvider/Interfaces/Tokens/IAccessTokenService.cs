using AuthenticationProvider.Models.Data;

namespace AuthenticationProvider.Interfaces.Tokens;

/// <summary>
/// Defines methods for generating, validating, and managing access tokens for user authentication.
/// This service handles tasks such as generating new tokens, checking token validity, and revoking expired or unused tokens.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Generates a new access token for the specified user.
    /// The token is used for authentication and is valid for a limited period.
    /// </summary>
    /// <param name="user">The user for whom the access token is generated.</param>
    /// <returns>A string representing the newly generated access token.</returns>
    string GenerateAccessToken(ApplicationUser user);

    /// <summary>
    /// Retrieves the user ID from the provided access token.
    /// This method is useful for extracting user information from a token.
    /// </summary>
    /// <param name="token">The access token from which to extract the user ID.</param>
    /// <returns>The user ID if the token is valid; otherwise, null.</returns>
    string GetUserIdFromToken(string token);

    /// <summary>
    /// Checks if the provided access token is valid by verifying its signature and claims.
    /// Valid tokens are not blacklisted and have not expired.
    /// </summary>
    /// <param name="token">The token to be validated.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool IsTokenValid(string token);

    /// <summary>
    /// Revokes the access token associated with the specified user.
    /// Once revoked, the token can no longer be used for authentication.
    /// </summary>
    /// <param name="user">The user whose token should be revoked.</param>
    void RevokeAccessToken(ApplicationUser user);
}
