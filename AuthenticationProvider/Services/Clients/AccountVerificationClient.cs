using Newtonsoft.Json;
using System.Text;
using AuthenticationProvider.Interfaces;

namespace AuthenticationProvider.Services.Clients;

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
    /// Sends an account verification request (token) to the Email Provider that creates and sends the email
    /// </summary>
    /// <param name="token">The verification token to send in the email.</param>
    /// <returns>True if the email was sent successfully; otherwise, false.</returns>
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

            // Create the request payload which contains the token (no email needed)
            var requestPayload = new { Token = token };
            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to AccountVerificationProvider endpoint: {Endpoint}", _accountVerificationEndpoint);

            var response = await _httpClient.PostAsync(_accountVerificationEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Account verification email sent successfully.");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send account verification email.");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "An error occurred while sending the account verification email.");
            Console.WriteLine("Ett problem uppstod vid sändning av verifieringsmejlet.");
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
