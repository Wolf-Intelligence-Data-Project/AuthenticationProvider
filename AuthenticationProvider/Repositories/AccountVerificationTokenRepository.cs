using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountVerificationTokenRepository> _logger;

    public AccountVerificationTokenRepository(ApplicationDbContext context, ILogger<AccountVerificationTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Retrieve the last email verification token by company GUID
    public async Task<string> GetLastEmailVerificationTokenAsync(Guid companyId)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            return company?.LastEmailVerificationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving last email verification token. Error: {ex.Message}");
            throw new InvalidOperationException("Det gick inte att hämta verifieringstoken. Försök igen senare.", ex); 
        }
    }

    // Update the email verification token by company GUID
    public async Task<bool> UpdateLastEmailVerificationTokenAsync(Guid companyId, string token)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                return false; // Company not found
            }

            company.LastEmailVerificationToken = string.IsNullOrEmpty(token) ? null : token;
            await _context.SaveChangesAsync();
            return true; // Successfully updated or deleted
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating email verification token. Error: {ex.Message}");
            throw new InvalidOperationException("Det gick inte att uppdatera verifieringstoken. Försök igen senare.", ex);
        }
    }

    // Save the email verification token by company GUID
    public async Task SaveEmailVerificationTokenAsync(Guid companyId, string token)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                throw new ArgumentException("Företaget kunde inte hittas.");
            }

            company.LastEmailVerificationToken = token;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving email verification token.");
            throw new InvalidOperationException("Det gick inte att spara verifieringstoken. Försök igen senare.", ex);
        }
    }

    // Revoke and delete the login session token by email
    public async Task<bool> RevokeAndDeleteLoginSessionTokenAsync(string email)
    {
        try
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Email == email);
            if (company == null)
            {
                return false; // Company not found
            }

            // Clear and delete the login session token
            company.LastLoginSessionToken = null;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error revoking and deleting login session token. Error: {ex.Message}");
            throw new InvalidOperationException("Det gick inte att återkalla inloggningstoken. Försök igen senare.", ex);
        }
    }
}
