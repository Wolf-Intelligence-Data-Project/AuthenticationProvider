namespace AuthenticationProvider.Interfaces.Services;

public interface ISendVerificationClient
{
    // Send the verification email with the provided token
    Task<bool> SendVerificationEmailAsync(string token);
}
