using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Interfaces.Utilities.Security;

public interface IResetPasswordService
{
    /// <summary>
    /// Sends an email containing a password reset link with the provided token.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <returns>True if the email was sent successfully; otherwise, false.</returns>
    Task<bool> SendResetPasswordEmailAsync(string token);

    /// <summary>
    /// Resets the company's password using the provided token and new password.
    /// </summary>
    /// <param name="resetPasswordRequest">The request containing the reset token and new password.</param>
    /// <returns>True if the password was successfully reset; otherwise, false.</returns>
    Task<bool> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);

}
