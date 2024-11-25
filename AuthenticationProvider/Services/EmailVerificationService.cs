using AuthenticationProvider.Interfaces;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly EmailVerificationClient _emailVerificationClient;

    public EmailVerificationService(EmailVerificationClient emailVerificationClient)
    {
        _emailVerificationClient = emailVerificationClient;
    }

    // Now only accepts token
    public async Task<bool> SendVerificationEmailAsync(string token)
    {
        try
        {
            // Call the EmailVerificationClient to send the token via the EmailVerificationProvider
            return await _emailVerificationClient.SendVerificationEmailAsync(token);
        }
        catch (Exception)
        {
            // Handle any exceptions (e.g., email service not available)
            return false;
        }
    }
}
