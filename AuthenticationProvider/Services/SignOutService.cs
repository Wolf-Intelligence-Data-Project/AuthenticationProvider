using AuthenticationProvider.Interfaces;
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
        // Perform logout-related actions like clearing session, cookies, etc.
        var company = await _companyRepository.GetByEmailAsync(email);
        if (company == null)
        {
            return false; // Company not found, can't sign out
        }

        // Revoke login session token as part of sign-out process
        company.LastLoginSessionToken = string.Empty;
        await _companyRepository.UpdateAsync(company);

        _logger.LogInformation($"User logged out: {company.CompanyName}, Email: {email}");
        return true;
    }
}
