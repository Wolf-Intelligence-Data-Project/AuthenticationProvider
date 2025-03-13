using AuthenticationProvider.Models.Responses;

namespace AuthenticationProvider.Interfaces.Utilities.Security;

public interface IAccountVerificationService
    {
     /// <summary>
     /// Sends an email verification message containing a verification token.
     /// </summary>
     /// <param name="token">The account verification token.</param>
     /// <returns>A service result indicating success or failure.</returns>
    Task<ServiceResult> SendVerificationEmailAsync(string token);

    /// <summary>
    /// Verifies a user’s email address using a provided verification token.
    /// </summary>
    /// <param name="token">The verification token received via email.</param>
    /// <returns>A service result indicating whether the verification was successful.</returns>
    Task<ServiceResult> VerifyEmailAsync(string token);

    Task<ServiceResult> ResendVerificationEmailAsync(string email);

}
