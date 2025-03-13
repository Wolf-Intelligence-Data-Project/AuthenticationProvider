namespace AuthenticationProvider.Interfaces.Utilities;

/// <summary>
/// Handles the sign-out process for a user.
/// </summary>
public interface ISignOutService
{
    /// <summary>
    /// Signs out a user by invalidating the provided token.
    /// </summary>
    /// <param name="token">The authentication token to be invalidated.</param>
    /// <returns>True if sign-out is successful; otherwise, false.</returns>
    Task<bool> SignOutAsync(string token);
}
