using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Azure;
using Microsoft.EntityFrameworkCore;
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

    // Check if a company exists by organisation number or email
    public async Task<bool> CompanyExistsAsync(string organisationNumber, string email)
    {
        try
        {
            return await _context.Set<Company>()
                .AnyAsync(c => c.OrganisationNumber == organisationNumber || c.Email == email); // Check for either OrganisationNumber or Email
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error checking company existence: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Add a new company
    public async Task AddAsync(Company company)
    {
        try
        {
            // Check if company with same organisation number or email already exists
            bool companyExists = await _context.Set<Company>()
                .AnyAsync(c => c.OrganisationNumber == company.OrganisationNumber || c.Email == company.Email);

            if (companyExists)
            {
                throw new InvalidOperationException("Företaget med samma organisationsnummer eller e-postadress existerar redan.");  // "The company with the same organisation number or email already exists."
            }

            await _context.Set<Company>().AddAsync(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error adding company: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Update an existing company
    public async Task UpdateAsync(Company company)
    {
        try
        {
            var existingCompany = await _context.Set<Company>().FindAsync(company.Id);

            if (existingCompany == null)
            {
                throw new InvalidOperationException("Företaget finns inte.");  // "The company does not exist."
            }

            _context.Set<Company>().Update(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error updating company: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Retrieve a company by email
    public async Task<Company> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error retrieving company by email: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }
    // Add this method to the CompanyRepository
    public async Task<Company> GetByIdAsync(Guid companyId)
    {
        try
        {
            return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Id == companyId);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error retrieving company by ID: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }
    // Delete a company by its ID
    public async Task DeleteAsync(Guid companyId)
    {
        try
        {
            var company = await _context.Companies
                .Include(c => c.Addresses)  // Ensure related addresses are loaded
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                throw new InvalidOperationException("Företaget finns inte.");
            }

            // Remove all associated addresses
            _context.Addresses.RemoveRange(company.Addresses);

            // Remove the company
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not delete.");
        }
    }

}