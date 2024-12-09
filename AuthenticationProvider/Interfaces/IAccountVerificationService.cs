using AuthenticationProvider.Models.Tokens;
using AuthenticationProvider.Services;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationService
{
    // Sends the verification email with the provided token
    Task<bool> SendVerificationEmailAsync(string token);

    // Verifies the email using the provided token and updates the company status
    Task<VerificationResult> VerifyEmailAsync(string token);
}
