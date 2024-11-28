using AuthenticationProvider.Data;
using AuthenticationProvider.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Check if a company exists by organisation number and email
    public async Task<bool> CompanyExistsAsync(string organisationNumber, string email)
    {
        return await _context.Set<Company>().AnyAsync(c => c.OrganisationNumber == organisationNumber && c.Email == email);
    }

    // Add a new company
    public async Task AddAsync(Company company)
    {
        bool companyExists = await _context.Set<Company>()
            .AnyAsync(c => c.OrganisationNumber == company.OrganisationNumber && c.Email == company.Email);

        if (companyExists)
        {
            throw new InvalidOperationException("Företaget med samma organisationsnummer och e-postadress existerar redan.");
        }

        await _context.Set<Company>().AddAsync(company);
        await _context.SaveChangesAsync();  // Save changes here
    }

    // Update an existing company
    public async Task UpdateAsync(Company company)
    {
        var existingCompany = await _context.Set<Company>().FindAsync(company.Id);

        if (existingCompany == null)
        {
            throw new InvalidOperationException("Företaget finns inte.");
        }

        _context.Set<Company>().Update(company);
        await _context.SaveChangesAsync();  // Save changes here
    }

    // Retrieve a company by email
    public async Task<Company> GetByEmailAsync(string email)
    {
        return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
    }

    // Retrieve a company by organisation number
    public async Task<Company> GetByOrganisationNumberAsync(string organisationNumber)
    {
        return await _context.Set<Company>().FirstOrDefaultAsync(c => c.OrganisationNumber == organisationNumber);
    }

    // Retrieve a company by GUID (Id)
    public async Task<Company> GetByGuidAsync(Guid companyId)
    {
        return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Id == companyId);
    }

    // Retrieve the last email verification token for a company
    public async Task<string> GetLastEmailVerificationTokenAsync(string email)
    {
        var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);

        return company?.LastEmailVerificationToken;
    }

    // Update the email verification token for a company
    public async Task UpdateEmailVerificationTokenAsync(string email, string token)
    {
        var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);

        if (company == null)
        {
            throw new InvalidOperationException("Företag med denna e-postadress hittades inte.");
        }

        company.LastEmailVerificationToken = token;
        _context.Set<Company>().Update(company);
        await _context.SaveChangesAsync();  // Save changes here
    }

    // Get a company for email verification
    public async Task<Company> GetCompanyForVerificationAsync(string email)
    {
        return await _context.Set<Company>()
            .FirstOrDefaultAsync(c => c.Email == email && c.LastEmailVerificationToken != null);
    }

    // Revoke email verification token for a company
    public async Task RevokeEmailVerificationTokenAsync(string email)
    {
        var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);

        if (company == null)
        {
            throw new InvalidOperationException("Företag med denna e-postadress hittades inte.");
        }

        company.LastEmailVerificationToken = null;
        _context.Set<Company>().Update(company);
        await _context.SaveChangesAsync();  // Save changes here
    }
}
