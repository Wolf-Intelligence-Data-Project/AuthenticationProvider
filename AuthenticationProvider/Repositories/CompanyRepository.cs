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

    public async Task<bool> CompanyExistsAsync(string organisationNumber, string email)
    {
        return await _context.Companies
            .AnyAsync(c => c.OrganisationNumber == organisationNumber || c.Email == email);
    }

    public async Task<Company> GetCompanyByIdAsync(int companyId)
    {
        return await _context.Companies
            .Include(c => c.Addresses)  // Load addresses if necessary
            .FirstOrDefaultAsync(c => c.Id == companyId);
    }

    public async Task AddAsync(Company company)
    {
        await _context.Companies.AddAsync(company);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Company company)
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
    }
}
