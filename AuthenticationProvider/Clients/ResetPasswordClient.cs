using AuthenticationProvider.Interfaces.Clients;
using Newtonsoft.Json;
using System.Text;

namespace AuthenticationProvider.Clients;

/// <summary>
/// Client responsible for dispatching a reset password token to an external provider, 
/// which then delivers it to the user via email. This client sends an HTTP request 
/// containing the reset password token to the configured provider endpoint.
/// </summary>
public class ResetPasswordClient : IResetPasswordClient
{
    private readonly HttpClient _httpClient;
    private readonly string _resetPasswordEndpoint;
    private readonly ILogger<ResetPasswordClient> _logger;

    public ResetPasswordClient(HttpClient httpClient, IConfiguration configuration, ILogger<ResetPasswordClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _resetPasswordEndpoint = configuration["ResetPasswordProvider:Endpoint"]
            ?? throw new ArgumentNullException(nameof(configuration), "Reset password endpoint is not configured.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the external reset password provider.</param>
    /// <param name="configuration">Configuration containing the endpoint URL for the reset password provider.</param>
    /// <param name="logger">Logger for recording request attempts, warnings, and errors.</param>
    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Provided reset password token is null or empty.");
            return false;
        }

        try
        {
            _logger.LogInformation("Sending reset password request to {Endpoint}", _resetPasswordEndpoint);

            var requestPayload = new { Token = token };
            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_resetPasswordEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Reset password email sent successfully.");
                return true;
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to send reset password email. Status Code: {StatusCode}, Response: {ErrorResponse}",
                response.StatusCode, errorResponse);
            return false;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request error while sending reset password email.");
            return false;
        }
        catch (TaskCanceledException timeoutEx)
        {
            _logger.LogError(timeoutEx, "Request timed out while sending reset password email.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending reset password email.");
            return false;
        }
    }
}
