namespace AuthenticationProvider.Interfaces;

public interface IEmailVerificationProvider
{
    Task SendVerificationEmailAsync(string email, string token);
}
