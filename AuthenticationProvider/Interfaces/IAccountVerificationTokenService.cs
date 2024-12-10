using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Tokens;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationTokenService
{
    /// <summary>
    /// Creates a new account verification token for a given company.
    /// </summary>
    /// <param name="companyId">The ID of the company for which the token is generated.</param>
    /// <returns>The generated token string.</returns>
    Task<string> CreateAccountVerificationTokenAsync(Guid companyId);

    /// <summary>
    /// Validates an account verification token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    Task<bool> ValidateAccountVerificationTokenAsync(string token);

    /// <summary>
    /// Marks the account verification token as used after the verification process is completed.
    /// </summary>
    /// <param name="token">The token to mark as used.</param>
    Task MarkAccountVerificationTokenAsUsedAsync(string token);

    /// <summary>
    /// Retrieves a valid account verification token, ensuring it's not expired and not already used.
    /// </summary>
    /// <param name="token">The token to retrieve and validate.</param>
    /// <returns>A valid account verification token if available, or null if invalid/expired/used.</returns>
    Task<AccountVerificationToken> GetValidAccountVerificationTokenAsync(string token);

    /// <summary>
    /// Deletes all account verification tokens associated with a company.
    /// </summary>
    /// <param name="companyId">The ID of the company whose tokens should be deleted.</param>
    Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId);
}
