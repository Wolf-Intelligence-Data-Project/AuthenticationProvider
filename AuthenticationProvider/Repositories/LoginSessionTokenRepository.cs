using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class LoginSessionTokenRepository : ILoginSessionTokenRepository
{
    private readonly ApplicationDbContext _context;

    public LoginSessionTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Retrieve the login session token by company GUID
    public async Task<string> GetLoginSessionTokenAsync(Guid companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        return company?.LastLoginSessionToken;
    }

    // Update the login session token by company GUID
    public async Task<bool> UpdateLoginSessionTokenAsync(Guid companyId, string token)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
        {
            return false; // Company not found
        }

        company.LastLoginSessionToken = string.IsNullOrEmpty(token) ? null : token;
        await _context.SaveChangesAsync();
        return true; // Successfully updated or deleted
    }

    // Save the login session token by company GUID
    public async Task SaveLoginSessionTokenAsync(Guid companyId, string token)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
        {
            throw new ArgumentException($"No company found for GUID: {companyId}");
        }

        company.LastLoginSessionToken = token;
        await _context.SaveChangesAsync();
    }
}
