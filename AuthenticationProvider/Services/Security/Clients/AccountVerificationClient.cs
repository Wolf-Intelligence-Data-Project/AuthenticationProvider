using Newtonsoft.Json;
using System.Text;
using AuthenticationProvider.Interfaces.Services.Security.Clients;

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
    public async Task<bool> SendVerificationEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Provided token is null or empty.");
            return false;
        }

        try
        {
            _logger.LogInformation("Preparing to send verification email.");

            var requestPayload = new { Token = token };
            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to AccountVerificationProvider endpoint.");

            var response = await _httpClient.PostAsync(_accountVerificationEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Account verification email sent successfully.");
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    _logger.LogWarning("Client-side error when sending verification email. Status: {StatusCode}, Response: {ResponseContent}",
                                        response.StatusCode, responseContent);
                }
                else if ((int)response.StatusCode >= 500)
                {
                    _logger.LogError("Server-side error when sending verification email. Status: {StatusCode}, Response: {ResponseContent}",
                                      response.StatusCode, responseContent);
                }

                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error when sending verification email.");
            throw; // Consider rethrowing if the caller should handle it
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending verification email.");
            throw;
        }
    }
}
