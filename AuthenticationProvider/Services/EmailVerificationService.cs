using AuthenticationProvider.Interfaces;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly EmailVerificationClient _emailVerificationClient;

    public EmailVerificationService(EmailVerificationClient emailVerificationClient)
    {
        _emailVerificationClient = emailVerificationClient;
    }

    public async Task<bool> SendVerificationEmailAsync(string email, string token)
    {
        try
        {
            // Call the EmailVerificationClient to send the email via the EmailVerificationProvider, passing both email and token
            return await _emailVerificationClient.SendVerificationEmailAsync(email, token);
        }
        catch (Exception)
        {
            // Handle any exceptions (e.g., email service not available)
            return false;
        }
    }
}
