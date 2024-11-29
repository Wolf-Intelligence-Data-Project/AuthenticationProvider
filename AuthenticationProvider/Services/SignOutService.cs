using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AuthenticationProvider.Services;

public class SignOutService : ISignOutService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<SignOutService> _logger;

    public SignOutService(ICompanyRepository companyRepository, ILogger<SignOutService> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<bool> SignOutAsync(string email)
    {
        try
        {
            // Perform logout-related actions like clearing session, cookies, etc.
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found.");
                return false; // Company not found, can't sign out
            }

            // Revoke login session token as part of sign-out process
            company.LastLoginSessionToken = string.Empty;
            await _companyRepository.UpdateAsync(company);

            _logger.LogInformation($"User logged out: {company.CompanyName}");
            return true;
        }
        catch (Exception ex)
        {
            // Log the error message without exposing sensitive information
            _logger.LogError($"Error occurred during sign-out process. Error: {ex.Message}");
            throw new InvalidOperationException("Det uppstod ett problem vid utloggning. Försök igen senare.", ex); // User-friendly message in Swedish
        }
    }
}
