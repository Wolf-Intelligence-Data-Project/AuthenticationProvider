using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Interfaces.Services.Security.Clients;

/// <summary>
/// Defines a contract for an external client responsible for sending 
/// password reset emails. Implementing classes should handle communication 
/// with an external provider that delivers the reset password email to the user.
/// </summary>
public interface IResetPasswordClient
{
    /// <summary>
    /// Sends a password reset request containing a unique reset token 
    /// to an external provider, which will then deliver the email to the user.
    /// </summary>
    /// <param name="resetId">A unique token used to authorize the password reset request.</param>
    /// <returns>True if the request was successfully processed by the provider; otherwise, false.</returns>
    Task<bool> SendResetPasswordEmailAsync(SendResetPasswordRequest sendResetPasswordRequest);
}
