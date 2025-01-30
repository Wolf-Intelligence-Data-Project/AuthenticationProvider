using AuthenticationProvider.Models.Data.Requests;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface IAccountSecurityService
{
    /// <summary>
    /// Changes the email address for a company.
    /// </summary>
    /// <param name="emailChangeRequest">The request containing the token and new email address.</param>
    /// <returns>True if the email was changed successfully, otherwise false.</returns>
    Task<bool> ChangeEmailAsync(EmailChangeRequest emailChangeRequest);

    /// <summary>
    /// Changes the password for a company.
    /// </summary>
    /// <param name="passwordChangeRequest">The request containing the token and new passwords.</param>
    /// <returns>True if the password was changed successfully, otherwise false.</returns>
    Task<bool> ChangePasswordAsync(PasswordChangeRequest passwordChangeRequest);
}
