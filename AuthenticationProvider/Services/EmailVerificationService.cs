using AuthenticationProvider.Interfaces;

namespace AuthenticationProvider.Services;

public class EmailVerificationService(EmailVerificationClient emailVerificationClient) : IEmailVerificationService
{
    private readonly EmailVerificationClient _emailVerificationClient = emailVerificationClient;

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