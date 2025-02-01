using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Interfaces.Tokens;

/// <summary>
/// Provides methods for managing account verification tokens for companies.
/// This service is responsible for generating, validating, marking as used, and deleting tokens
/// related to account verification processes, ensuring secure and proper token management.
/// </summary>
public interface IAccountVerificationTokenService
{
    /// <summary>
    /// Generates a new account verification token for the specified company.
    /// The token is used to verify the company's account and is associated with the company's email.
    /// The token will be valid for a limited period.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company requesting verification.</param>
    /// <returns>A string representing the generated token.</returns>
    Task<string> CreateAccountVerificationTokenAsync(Guid companyId);

    /// <summary>
    /// Validates the provided account verification token to ensure it is legitimate and not expired.
    /// Verifies that the token corresponds to an account verification process and has not been used or revoked.
    /// </summary>
    /// <param name="request">The request containing the token to be validated.</param>
    /// <returns>A response indicating whether the token is valid or expired, along with appropriate status codes.</returns>
    Task<IActionResult> ValidateAccountVerificationTokenAsync(TokenRequest request);

    /// <summary>
    /// Marks the specified account verification token as used. Once marked, the token can no longer be used for verification.
    /// This operation is typically performed after successful account verification.
    /// </summary>
    /// <param name="token">The token that needs to be marked as used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAccountVerificationTokenAsUsedAsync(string token);

    /// <summary>
    /// Retrieves the account verification token if it is still valid and not expired or used.
    /// If the token has been used or expired, it will return null.
    /// </summary>
    /// <param name="token">The token to be retrieved for validation.</param>
    /// <returns>A task representing the asynchronous operation, with the account verification token entity if valid, otherwise null.</returns>
    Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string token);

    /// <summary>
    /// Deletes all account verification tokens associated with a specific company.
    /// This operation ensures that there are no lingering or unused tokens for the company.
    /// </summary>
    /// <param name="companyId">The unique identifier of the company whose tokens should be deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId);
}
