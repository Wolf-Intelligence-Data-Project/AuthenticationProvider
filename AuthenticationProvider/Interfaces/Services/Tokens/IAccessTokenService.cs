﻿using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Services.Tokens;

/// <summary>
/// Interface for managing access tokens for users. Includes generation, validation, and revocation of JWT tokens.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Generates a new access token for the specified user.
    /// </summary>
    /// <param name="userEntity">The user for whom the access token is being generated.</param>
    /// <returns>A JWT access token as a string.</returns>
    Task<string> GenerateAccessToken(UserEntity user);

    /// <summary>
    /// Retrieves the user ID from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to extract the user ID from.</param>
    /// <returns>The user ID associated with the token, or null if unable to parse.</returns>
    string GetUserIdFromToken(string token);

    /// <summary>
    /// Validates whether the specified token is valid and not blacklisted.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid, false if it is blacklisted or invalid.</returns>
    (bool isAuthenticated, bool isEmailVerified) ValidateAccessToken(string token);

    /// <summary>
    /// Revokes the access token for the specified user by adding it to the blacklist and removing it from in-memory storage.
    /// </summary>
    /// <param name="user">The user whose access token is to be revoked.</param>
    Task RevokeAndBlacklistAccessToken(string userId);

    void CleanUpExpiredTokens();
}
