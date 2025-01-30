using AuthenticationProvider.Interfaces.Clients;
using Newtonsoft.Json;
using System.Text;

namespace AuthenticationProvider.Clients;

public class ResetPasswordClient : IResetPasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly string _resetPasswordEndpoint;
    private readonly ILogger<ResetPasswordClient> _logger;

    public ResetPasswordClient(HttpClient httpClient, IConfiguration configuration, ILogger<ResetPasswordClient> logger)
    {
        _httpClient = httpClient;
        _resetPasswordEndpoint = configuration["ResetPasswordProvider:Endpoint"]
            ?? throw new ArgumentNullException("Endpoint is not configured.");
        _logger = logger;
    }

    /// <summary>
    /// Sends a reset password email with the provided token.
    /// </summary>
    /// <param name="token">The token used to identify the password reset request.</param>
    /// <returns>True if the reset email was sent successfully, otherwise false.</returns>
    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Provided token is null or empty.");
            return false;
        }

        try
        {
            _logger.LogInformation("Preparing to send reset password email.");

            // Create the payload for the request (token)
            var requestPayload = new { Token = token };
            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to ResetPasswordProvider endpoint: {Endpoint}", _resetPasswordEndpoint);

            var response = await _httpClient.PostAsync(_resetPasswordEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Reset password email sent successfully.");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send reset password email.");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "An error occurred while sending the reset password email.");
            Console.WriteLine("Ett problem uppstod vid sändning av återställningsmejlet.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            Console.WriteLine("Ett oväntat fel inträffade.");
            return false;
        }
    }
}
