namespace AuthenticationProvider.Interfaces.Services.Security.Clients;

/// <summary>
/// Defines a contract for an external client responsible for sending 
/// account verification emails. Implementing classes should handle 
/// communication with an external provider that delivers the verification email.
/// </summary>
public interface IAccountVerificationClient
{
    /// <summary>
    /// Sends an account verification request containing a verification token 
    /// to an external provider, which will then deliver the email to the user.
    /// </summary>
    /// <param name="token">A unique token used to verify the user's account.</param>
    /// <returns>True if the request was successfully processed by the provider; otherwise, false.</returns>
    Task<bool> SendVerificationEmailAsync(string token);
}
