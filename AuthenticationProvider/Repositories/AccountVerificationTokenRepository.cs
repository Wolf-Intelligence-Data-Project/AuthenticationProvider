using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public AccountVerificationTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Retrieve the last email verification token by company GUID
    public async Task<string> GetLastEmailVerificationTokenAsync(Guid companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        return company?.LastEmailVerificationToken;
    }

    // Update the email verification token by company GUID
    public async Task<bool> UpdateLastEmailVerificationTokenAsync(Guid companyId, string token)
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

    // Save the email verification token by company GUID
    public async Task SaveEmailVerificationTokenAsync(Guid companyId, string token)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
        {
            throw new ArgumentException($"No company found for GUID: {companyId}");
        }

        company.LastEmailVerificationToken = token;
        await _context.SaveChangesAsync();
    }
    public async Task<bool> RevokeAndDeleteLoginSessionTokenAsync(string email)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Email == email);
        if (company == null)
        {
            return false; // Company not found
        }

        // Clear and delete the login session token
        company.LastLoginSessionToken = null; // Set to null to revoke the token
        await _context.SaveChangesAsync();
        return true; // Successfully revoked and deleted the token
    }

}
