using Newtonsoft.Json;
using System.Text;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Services.Security.Clients;

/// <summary>
/// Handles dispatching account verification tokens to an external service responsible for sending emails to users.
/// This client communicates with the configured verification provider to request email delivery.
/// </summary>
public class AccountVerificationClient : IAccountVerificationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accountVerificationEndpoint;
    private readonly ILogger<AccountVerificationClient> _logger;
    public AccountVerificationClient(HttpClient httpClient, IConfiguration configuration, ILogger<AccountVerificationClient> logger)
    {
        _httpClient = httpClient;
        _accountVerificationEndpoint = configuration["AccountVerificationProvider:Endpoint"]
            ?? throw new ArgumentNullException("Endpoint is not configured.");

        _logger = logger;
    }

    /// <summary>
    /// Sends an account verification request containing a unique token to an external email provider.
    /// The external provider is responsible for delivering the verification email.
    /// </summary>
    /// <param name="token">The verification token required for confirming user identity.</param>
    /// <returns>
    /// <c>true</c> if the request was successfully processed by the external provider; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if a network-related issue occurs.</exception>
    /// <exception cref="Exception">Thrown for unexpected failures during the request.</exception>
    public async Task<bool> SendVerificationEmailAsync( SendVerificationRequest sendVerificationRequest)
    {
        if (sendVerificationRequest == null)
        {
            _logger.LogWarning("Provided token is null or empty.");
            return false;
        }

        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(sendVerificationRequest), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_accountVerificationEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Account verification email sent successfully.");
                return true;
            }
            // Log the error response if the request failed.
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
