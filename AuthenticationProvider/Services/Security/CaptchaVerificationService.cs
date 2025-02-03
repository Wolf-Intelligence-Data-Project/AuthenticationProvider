using AuthenticationProvider.Interfaces.Security;
using AuthenticationProvider.Models;

namespace AuthenticationProvider.Services.Security;

public class CaptchaVerificationService : ICaptchaVerificationService
{
    private readonly string _recaptchaSecretKey;
    private readonly HttpClient _httpClient;

    public CaptchaVerificationService(IConfiguration configuration, HttpClient httpClient)
    {
        _recaptchaSecretKey = configuration["GoogleReCaptcha:SecretKey"];
        _httpClient = httpClient;
    }

    public async Task<bool> VerifyCaptchaAsync(string captchaToken)
    {
        var response = await _httpClient.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={_recaptchaSecretKey}&response={captchaToken}",
            null
        );

        if (!response.IsSuccessStatusCode)
            return false;

        var captchaResponse = await response.Content.ReadFromJsonAsync<CaptchaVerificationResponse>();
        return captchaResponse?.Success == true;
    }
}
