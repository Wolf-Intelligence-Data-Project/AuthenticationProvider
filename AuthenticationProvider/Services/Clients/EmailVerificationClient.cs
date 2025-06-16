using Newtonsoft.Json;
using System.Text;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Models.Dtos;

namespace AuthenticationProvider.Services.Clients;

/// <summary>
/// Handles dispatching email verification tokens to an external service responsible for sending emails to users.
/// This client communicates with the configured verification provider to request email delivery.
/// </summary>
public class EmailVerificationClient : IEmailVerificationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _emailVerificationEndpoint;
    private readonly ILogger<EmailVerificationClient> _logger;
    public EmailVerificationClient(HttpClient httpClient, IConfiguration configuration, ILogger<EmailVerificationClient> logger)
    {
        _httpClient = httpClient;
        _emailVerificationEndpoint = configuration["EmailVerificationProvider:Endpoint"]
            ?? throw new ArgumentNullException("Endpoint is not configured.");

        _logger = logger;
    }

    /// <summary>
    /// Sends an email verification request containing a unique token to an external email provider.
    /// The external provider is responsible for delivering the verification email.
    /// </summary>
    /// <param name="token">The verification token required for confirming user identity.</param>
    /// <returns>
    /// <c>true</c> if the request was successfully processed by the external provider; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if a network-related issue occurs.</exception>
    /// <exception cref="Exception">Thrown for unexpected failures during the request.</exception>
    public async Task<bool> SendVerificationEmailAsync( EmailVerificationDto sendVerificationRequest)
    {
        if (sendVerificationRequest == null)
        {
            _logger.LogWarning("Provided token is null or empty.");
            return false;
        }

        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(sendVerificationRequest), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_emailVerificationEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email verification email sent successfully.");
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
