using AuthenticationProvider.Interfaces.Services;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

public class SendVerificationClient : ISendVerificationClient
{
    private readonly HttpClient _httpClient;

    // Constructor to inject the HttpClient
    public SendVerificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Implementation of the SendVerificationEmailAsync method
    public async Task<bool> SendVerificationEmailAsync(string token)
    {
        var requestPayload = new
        {
            Token = token
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

        // Call the external email service (Azure Function or another service)
        var response = await _httpClient.PostAsync("http://localhost:7092/api/SendVerificationEmail", content);

        return response.IsSuccessStatusCode;
    }
}
