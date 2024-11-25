using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace AuthenticationProvider.Services;

public class EmailVerificationClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<bool> SendVerificationEmailAsync(string token)
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