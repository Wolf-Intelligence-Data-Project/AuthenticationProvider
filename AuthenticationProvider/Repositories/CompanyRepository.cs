using AuthenticationProvider.Data;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

/// <summary>
/// Repository for managing company-related data operations in the database.
/// Implements the ICompanyRepository interface.
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyRepository> _logger;

    public CompanyRepository(ApplicationDbContext context, ILogger<CompanyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a company with the given organisation number or email already exists in the database.
    /// </summary>
    /// <param name="organisationNumber">The organisation number of the company.</param>
    /// <param name="email">The email of the company.</param>
    /// <returns>A boolean indicating whether the company exists.</returns>
    public async Task<bool> CompanyExistsAsync(string organisationNumber, string email)
    {
        try
        {
            return await _context.Set<CompanyEntity>()
                .AnyAsync(c => c.OrganizationNumber == organisationNumber || c.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if company exists.");
            throw;
        }
    }

    /// <summary>
    /// Adds a new company to the database. Throws an exception if a company with the same organisation number or email exists.
    /// </summary>
    /// <param name="company">The company entity to add.</param>
    public async Task AddAsync(CompanyEntity company)
    {
        try
        {
            // Check if the company already exists
            bool companyExists = await _context.Set<CompanyEntity>()
                .AnyAsync(c => c.OrganizationNumber == company.OrganizationNumber || c.Email == company.Email);

            if (companyExists)
            {
                throw new InvalidOperationException("Företaget med samma organisationsnummer eller e-postadress existerar redan.");
            }

            // Add the company to the database
            await _context.Set<CompanyEntity>().AddAsync(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding company.");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing company in the database. Throws an exception if the company does not exist.
    /// </summary>
    /// <param name="company">The updated company entity.</param>
    public async Task UpdateAsync(CompanyEntity company)
    {
        try
        {
            // Find the existing company by its ID
            var existingCompany = await _context.Set<CompanyEntity>().FindAsync(company.Id);

            if (existingCompany == null)
            {
                throw new InvalidOperationException("Företaget finns inte.");
            }
            _context.Set<CompanyEntity>().Update(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a company entity by its email address.
    /// </summary>
    /// <param name="email">The email address of the company.</param>
    /// <returns>The company entity if found, otherwise null.</returns>
    public async Task<CompanyEntity> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Set<CompanyEntity>().FirstOrDefaultAsync(c => c.Email == email);
        }
        catch (Exception ex)
        {
            // Log error details
            _logger.LogError(ex, "Error retrieving company by email.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a company entity by its unique ID.
    /// </summary>
    /// <param name="companyId">The unique ID of the company.</param>
    /// <returns>The company entity if found, otherwise null.</returns>
    public async Task<CompanyEntity> GetByIdAsync(Guid companyId)
    {
        try
        {
            return await _context.Set<CompanyEntity>().FirstOrDefaultAsync(c => c.Id == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by ID.");
            throw;
        }
    }

    /// <summary>
    /// Deletes a company from the database by its unique ID, including associated addresses.
    /// </summary>
    /// <param name="companyId">The unique ID of the company to delete.</param>
    public async Task DeleteAsync(Guid companyId)
    {
        try
        {
            // Retrieve the company with related addresses
            var company = await _context.Companies
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                throw new InvalidOperationException("Företaget finns inte.");
            }

            // Remove related addresses and the company itself
            _context.Addresses.RemoveRange(company.Addresses);
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company.");
            throw new InvalidOperationException("Could not delete.");
        }
    }
}
