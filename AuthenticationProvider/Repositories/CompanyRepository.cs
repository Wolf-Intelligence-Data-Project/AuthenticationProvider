using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.EntityFrameworkCore;
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
        await _context.Set<Company>().AddAsync(company);
        await _context.SaveChangesAsync();
    }

    // Update an existing company
    public async Task UpdateAsync(Company company)
    {
        _context.Set<Company>().Update(company);
        await _context.SaveChangesAsync();
    }

    // Retrieve a company by email
    public async Task<Company> GetByEmailAsync(string email)
    {
        return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
    }
}
