using AuthenticationProvider.Interfaces;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

public class ResetPasswordClient : IResetPasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly ILogger<ResetPasswordClient> _logger;

    public ResetPasswordClient(HttpClient httpClient, IConfiguration configuration, ILogger<ResetPasswordClient> logger)
    {
        _httpClient = httpClient;
        _endpoint = configuration["ResetPasswordProvider:Endpoint"]
                    ?? throw new ArgumentNullException("Endpoint is not configured.");
        _logger = logger;
    }

    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        var requestPayload = new { Token = token };
        var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_endpoint, content);

            // Log the response status and body in case of failure
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send reset password email. Status code: {response.StatusCode}. Error: {errorContent}");
                return false;
            }

            _logger.LogInformation("Reset password email sent successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending reset password email.");
            return false;
        }
    }
}
