namespace AuthenticationProvider.Interfaces;

public interface IEmailVerificationService
{
    Task<bool> SendVerificationEmailAsync(string email, string token);
}
