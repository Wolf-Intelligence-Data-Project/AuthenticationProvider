using AuthenticationProvider.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

public class EmailVerificationProvider : IEmailVerificationProvider
{
    private readonly string _smtpServer;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly int _smtpPort;

    public EmailVerificationProvider()
    {
        // These values should be configured in appsettings.json or via environment variables in production
        _smtpServer = "smtp.your-email-server.com";
        _smtpUsername = "your-email-username";
        _smtpPassword = "your-email-password";
        _smtpPort = 587; // Common SMTP port, change if needed
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var verificationLink = $"https://your-app.com/verify-email?token={token}";

        var message = new MailMessage
        {
            From = new MailAddress("no-reply@your-app.com"),
            Subject = "Email Verification",
            Body = $"Please click the following link to verify your email: {verificationLink}",
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true
        };

        await smtpClient.SendMailAsync(message);
    }
}
