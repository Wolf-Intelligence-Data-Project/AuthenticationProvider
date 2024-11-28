namespace AuthenticationProvider.Interfaces;

public interface ISendVerificationService
{
    // Generate and send the verification token to an external service
    Task<bool> SendVerificationEmailAsync(string token);
}
