using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        // Check if company with same organisation number and email already exists
        bool companyExists = await _context.Set<Company>()
            .AnyAsync(c => c.OrganisationNumber == company.OrganisationNumber && c.Email == company.Email);

        if (companyExists)
        {
            throw new InvalidOperationException("Företaget med samma organisationsnummer och e-postadress existerar redan.");  // "The company with the same organisation number and email already exists."
        }

        await _context.Set<Company>().AddAsync(company);
        await _context.SaveChangesAsync();
    }

    // Update an existing company
    public async Task UpdateAsync(Company company)
    {
        var existingCompany = await _context.Set<Company>().FindAsync(company.Id);

        if (existingCompany == null)
        {
            throw new InvalidOperationException("Företaget finns inte.");  // "The company does not exist."
        }

        _context.Set<Company>().Update(company);
        await _context.SaveChangesAsync();
    }

    // Retrieve a company by email
    public async Task<Company> GetByEmailAsync(string email)
    {
        return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
    }
}
