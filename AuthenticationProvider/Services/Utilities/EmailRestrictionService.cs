namespace AuthenticationProvider.Services.Utilities;

/// <summary>
/// Service responsible for checking if an email is restricted based on a list from the configuration.
/// </summary>
public class EmailRestrictionService : IEmailRestrictionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailRestrictionService> _logger;

    public EmailRestrictionService(IConfiguration configuration, ILogger<EmailRestrictionService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the given email is restricted based on a list defined in the configuration.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if the email is restricted, otherwise false.</returns>
    /// <exception cref="ArgumentException">Thrown when the email address is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an error occurs while checking the restricted emails.</exception>
    public bool IsRestrictedEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogError("Email address is null or empty.");

            throw new ArgumentException("E-postadress kan inte vara tom eller null.", nameof(email));
        }

        try
        {
            // Retrieve restricted emails list
            var restrictedEmails = _configuration.GetSection("RestrictedEmails").Get<string[]>();

            if (restrictedEmails == null || restrictedEmails.Length == 0)
            {
                return false;
            }

            bool isRestricted = restrictedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);

            return isRestricted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking if the email is restricted.");
            throw new InvalidOperationException("Ett fel inträffade när e-postadressen kontrollerades.", ex);
        }
    }
}
