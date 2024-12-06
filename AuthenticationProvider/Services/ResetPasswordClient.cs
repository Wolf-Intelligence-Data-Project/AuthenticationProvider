using AuthenticationProvider.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace AuthenticationProvider.Services;

public class ResetPasswordClient : IResetPasswordClient
{
    private readonly HttpClient _httpClient;

    public ResetPasswordClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        // Create the request payload with only the Token (no email)
        var requestPayload = new
        {
            Token = token
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

        // Send POST request to the EmailVerificationProvider endpoint
        var response = await _httpClient.PostAsync("http://localhost:7092/api/SendVerificationEmail", content);

        // Return true if the response is successful
        return response.IsSuccessStatusCode;
    }
}