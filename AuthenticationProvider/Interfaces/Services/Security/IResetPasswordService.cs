using System.Threading.Tasks;
using AuthenticationProvider.Models.Data.Dtos;

namespace AuthenticationProvider.Interfaces.Services.Security;

public interface IResetPasswordService
{
    /// <summary>
    /// Sends a reset password email using the provided token.
    /// </summary>
    /// <param name="token">The reset password token.</param>
    /// <returns>True if the email is sent successfully; otherwise, false.</returns>
    Task<bool> SendResetPasswordEmailAsync(string token);

    /// <summary>
    /// Handles the actual password reset when the user submits the new password.
    /// </summary>
    /// <param name="resetPasswordRequest">The request containing the token and the new password.</param>
    /// <returns>True if the password is successfully reset; otherwise, false.</returns>
    Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordRequest);
}
