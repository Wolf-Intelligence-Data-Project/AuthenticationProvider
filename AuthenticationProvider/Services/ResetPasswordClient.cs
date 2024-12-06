using AuthenticationProvider.Interfaces;
using Newtonsoft.Json;
using System.Text;

public class ResetPasswordClient : IResetPasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public ResetPasswordClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _endpoint = configuration["EmailVerificationProvider:Endpoint"]
                    ?? throw new ArgumentNullException("Endpoint is not configured.");
    }

    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        var requestPayload = new { Token = token };
        var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content);
        return response.IsSuccessStatusCode;
    }
}
