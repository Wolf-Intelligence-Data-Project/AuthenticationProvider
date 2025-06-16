using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Services.Tokens;

/// <summary>
/// Provides methods for managing email verification tokens for users.
/// This service is responsible for generating, validating, marking as used, and deleting tokens
/// related to email verification processes, ensuring secure and proper token management.
/// </summary>
public interface IEmailVerificationTokenService
{
    /// <summary>
    /// Generates a new email verification token for the specified user.
    /// The token is used to verify the user's email and is associated with the user's email.
    /// The token will be valid for a limited period.
    /// </summary>
    /// <param name="userId">The unique identifier of the user requesting verification.</param>
    /// <returns>A string representing the generated token.</returns>
    Task<TokenInfoModel> GenerateEmailVerificationTokenAsync(Guid userId);

    /// <summary>
    /// Validates the provided email verification token to ensure it is legitimate and not expired.
    /// Verifies that the token corresponds to an email verification process and has not been used or revoked.
    /// </summary>
    /// <param name="request">The request containing the token to be validated.</param>
    /// <returns>A response indicating whether the token is valid or expired, along with appropriate status codes.</returns>
    Task<bool> ValidateEmailVerificationTokenAsync(string tokenId);

    /// <summary>
    /// Marks the specified email verification token as used. Once marked, the token can no longer be used for verification.
    /// This operation is typically performed after successful email verification.
    /// </summary>
    /// <param name="token">The token that needs to be marked as used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkEmailVerificationTokenAsUsedAsync(Guid tokenId);

    /// <summary>
    /// Retrieves the email verification token if it is still valid and not expired or used.
    /// If the token has been used or expired, it will return null.
    /// </summary>
    /// <param name="verificationId">The token to be retrieved for validation.</param>
    /// <returns>A task representing the asynchronous operation, with the email verification token entity if valid, otherwise null.</returns>
    Task<EmailVerificationTokenEntity> GetValidEmailVerificationTokenAsync(string verificationId);
}
