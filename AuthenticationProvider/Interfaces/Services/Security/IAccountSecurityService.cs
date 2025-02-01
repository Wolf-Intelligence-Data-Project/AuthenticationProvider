using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Interfaces.Utilities.Security;

public interface IAccountSecurityService
{
    /// <summary>
    /// Attempts to change the email address of a company after validating the provided security token.
    /// The request must include a valid authentication token and the new email address.
    /// </summary>
    /// <param name="emailChangeRequest">The request containing the security token and the new email address.</param>
    /// <returns>True if the email change is successful after validation; otherwise, false.</returns>
    Task<bool> ChangeEmailAsync(EmailChangeRequest emailChangeRequest);

    /// <summary>
    /// Attempts to change the password of a company after verifying the provided security token.
    /// The request must contain the current authentication token, the new password, and a confirmation password.
    /// </summary>
    /// <param name="passwordChangeRequest">The request containing the authentication token and new password details.</param>
    /// <returns>True if the password change is successful after validation; otherwise, false.</returns>
    Task<bool> ChangePasswordAsync(PasswordChangeRequest passwordChangeRequest);

}
