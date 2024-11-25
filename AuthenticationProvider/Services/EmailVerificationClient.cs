using Newtonsoft.Json;
using System.Text;

public class EmailVerificationClient
{
    private readonly HttpClient _httpClient;

    public EmailVerificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendVerificationEmailAsync(string email, string token)
    {
        // Create the request payload with both Email and Token
        var requestPayload = new
        {
            Email = email,
            Token = token  // Include the token in the payload
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

        // Send POST request to the EmailVerificationProvider endpoint
        var response = await _httpClient.PostAsync("http://localhost:7092/api/SendVerificationEmail", content);

        return response.IsSuccessStatusCode;
    }

}
