namespace AuthenticationProvider.Interfaces.Security;

public interface ICaptchaVerificationService
{
    Task<bool> VerifyCaptchaAsync(string captchaToken);
}
