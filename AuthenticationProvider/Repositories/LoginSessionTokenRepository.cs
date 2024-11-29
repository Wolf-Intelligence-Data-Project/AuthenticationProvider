using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class LoginSessionTokenRepository : ILoginSessionTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginSessionTokenRepository> _logger;

    public LoginSessionTokenRepository(ApplicationDbContext context, ILogger<LoginSessionTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Retrieve the login session token by company GUID
    public async Task<string> GetLoginSessionTokenAsync(Guid companyId)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            return company?.LastLoginSessionToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving login session token for a company.");
            throw new InvalidOperationException("An error occurred while retrieving the login session token.");
        }
    }

    // Update the login session token by company GUID
    public async Task<bool> UpdateLoginSessionTokenAsync(Guid companyId, string token)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                _logger.LogWarning("Attempted to update login session token for a non-existent company.");
                return false; // Company not found
            }

            company.LastLoginSessionToken = string.IsNullOrEmpty(token) ? null : token;
            await _context.SaveChangesAsync();
            return true; // Successfully updated or deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating login session token for a company.");
            throw new InvalidOperationException("An error occurred while updating the login session token.");
        }
    }

    // Save the login session token by company GUID
    public async Task SaveLoginSessionTokenAsync(Guid companyId, string token)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                _logger.LogWarning("Attempted to save login session token for a non-existent company.");
                throw new InvalidOperationException("The specified company does not exist.");
            }

            company.LastLoginSessionToken = token;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving login session token for a company.");
            throw new InvalidOperationException("An error occurred while saving the login session token.");
        }
    }
}
