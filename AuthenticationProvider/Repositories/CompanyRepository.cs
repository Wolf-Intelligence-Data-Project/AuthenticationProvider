using AuthenticationProvider.Data;
using AuthenticationProvider.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyRepository> _logger;

    public CompanyRepository(ApplicationDbContext context, ILogger<CompanyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CompanyExistsAsync(string organisationNumber, string email)
    {
        try
        {
            return await _context.Set<Company>().AnyAsync(c => c.OrganisationNumber == organisationNumber && c.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if company exists.");
            throw new InvalidOperationException("An error occurred while checking company existence.");
        }
    }

    public async Task AddAsync(Company company)
    {
        try
        {
            bool companyExists = await _context.Set<Company>()
                .AnyAsync(c => c.OrganisationNumber == company.OrganisationNumber && c.Email == company.Email);

            if (companyExists)
            {
                throw new InvalidOperationException("Företaget med samma organisationsnummer och e-postadress existerar redan.");
            }

            await _context.Set<Company>().AddAsync(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding a company.");
            throw new InvalidOperationException("An error occurred while adding the company.");
        }
    }

    public async Task UpdateAsync(Company company)
    {
        try
        {
            var existingCompany = await _context.Set<Company>().FindAsync(company.Id);

            if (existingCompany == null)
            {
                throw new InvalidOperationException("Företaget finns inte.");
            }

            _context.Set<Company>().Update(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating a company.");
            throw new InvalidOperationException("An error occurred while updating the company.");
        }
    }

    public async Task<Company> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by email.");
            throw new InvalidOperationException("An error occurred while retrieving the company.");
        }
    }

    public async Task<Company> GetByOrganisationNumberAsync(string organisationNumber)
    {
        try
        {
            return await _context.Set<Company>().FirstOrDefaultAsync(c => c.OrganisationNumber == organisationNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by organisation number.");
            throw new InvalidOperationException("An error occurred while retrieving the company.");
        }
    }

    public async Task<Company> GetByGuidAsync(Guid companyId)
    {
        try
        {
            return await _context.Set<Company>().FirstOrDefaultAsync(c => c.Id == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company by GUID.");
            throw new InvalidOperationException("An error occurred while retrieving the company.");
        }
    }

    public async Task<string> GetLastEmailVerificationTokenAsync(string email)
    {
        try
        {
            var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);
            return company?.LastEmailVerificationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last email verification token.");
            throw new InvalidOperationException("An error occurred while retrieving the email verification token.");
        }
    }

    public async Task UpdateEmailVerificationTokenAsync(string email, string token)
    {
        try
        {
            var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);

            if (company == null)
            {
                throw new InvalidOperationException("Företag med denna e-postadress hittades inte.");
            }

            company.LastEmailVerificationToken = token;
            _context.Set<Company>().Update(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email verification token.");
            throw new InvalidOperationException("An error occurred while updating the email verification token.");
        }
    }

    public async Task<Company> GetCompanyForVerificationAsync(string email)
    {
        try
        {
            return await _context.Set<Company>()
                .FirstOrDefaultAsync(c => c.Email == email && c.LastEmailVerificationToken != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company for verification.");
            throw new InvalidOperationException("An error occurred while retrieving the company for verification.");
        }
    }

    public async Task RevokeEmailVerificationTokenAsync(string email)
    {
        try
        {
            var company = await _context.Set<Company>().FirstOrDefaultAsync(c => c.Email == email);

            if (company == null)
            {
                throw new InvalidOperationException("Företag med denna e-postadress hittades inte.");
            }

            company.LastEmailVerificationToken = null;
            _context.Set<Company>().Update(company);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking email verification token.");
            throw new InvalidOperationException("An error occurred while revoking the email verification token.");
        }
    }
}
