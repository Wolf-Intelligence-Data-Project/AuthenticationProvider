using AuthenticationProvider.Interfaces.Services;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AuthenticationProvider.Services;

public class SendVerificationClient : ISendVerificationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _verificationServiceUrl;
    private readonly ILogger<SendVerificationClient> _logger; // Inject the logger

    // Constructor to inject HttpClient, configuration, and ILogger
    public SendVerificationClient(HttpClient httpClient, IConfiguration configuration, ILogger<SendVerificationClient> logger)
    {
        _httpClient = httpClient;
        _verificationServiceUrl = configuration.GetValue<string>("ExternalServices:VerificationServiceUrl");
        _logger = logger;  // Assign the logger
    }

    // Implementation of the SendVerificationEmailAsync method with try-catch and logging
    public async Task<bool> SendVerificationEmailAsync(string token)
    {
        try
        {
            var requestPayload = new
            {
                Token = token
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");

            // Send the POST request
            var response = await _httpClient.PostAsync(_verificationServiceUrl, content);

            // Return whether the request was successful
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "HTTP Request error while sending verification email.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending verification email.");
            return false;
        }
    }
}
