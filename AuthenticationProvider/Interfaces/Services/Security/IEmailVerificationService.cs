using AuthenticationProvider.Models.Responses;

namespace AuthenticationProvider.Interfaces.Utilities.Security;

public interface IEmailVerificationService
    {
     /// <summary>
     /// Sends an email verification message containing a verification token.
     /// </summary>
     /// <param name="userId">The email verification token.</param>
     /// <returns>A service result indicating success or failure.</returns>
    Task<ServiceResult> PrepareAndSendVerificationAsync(string userId);

    /// <summary>
    /// Verifies a user’s email address using a provided verification token.
    /// </summary>
    /// <param name="verificationId">The verification token received via email.</param>
    /// <returns>A service result indicating whether the verification was successful.</returns>
    Task<ServiceResult> VerifyEmailAsync(string verificationId);

    Task<ServiceResult> ResendVerificationEmailAsync(string email);

}
