using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Data;
using System.Linq;
using AuthenticationProvider.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthenticationProvider.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AddressRepository> _logger;

    public AddressRepository(ApplicationDbContext dbContext, ILogger<AddressRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Add a new address to the database
    public async Task AddAsync(Address address)
    {
        try
        {
            // Ensure address uniqueness: Same street address, postal code, and city should not exist.
            bool isAddressUnique = !await _dbContext.Addresses
                .AnyAsync(a => a.StreetAddress == address.StreetAddress
                               && a.PostalCode == address.PostalCode
                               && a.City == address.City);

            if (!isAddressUnique)
            {
                throw new InvalidOperationException("Adressen existerar redan i systemet.");  // "The address already exists in the system."
            }

            // If adding primary address, check if company already has a primary address
            if (address.AddressType == "Primary")
            {
                bool hasPrimaryAddress = await _dbContext.Addresses
                    .AnyAsync(a => a.CompanyId == address.CompanyId && a.AddressType == "Primary");

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Företaget har redan en primäradress.");  // "The company already has a primary address."
                }
            }

            // Add the address to the database
            await _dbContext.AddAsync(address);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the error with a generic message, excluding sensitive data like company ID
            _logger.LogError("Error adding address. Error details: {ErrorMessage}", ex.Message);
            throw;  // Rethrow the exception to be handled by higher layers
        }
    }

    // Get an address by its ID
    public async Task<Address> GetByIdAsync(int id)
    {
        try
        {
            return await _dbContext.Addresses
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving address by ID. Error details: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    // Get all addresses for a specific company
    public async Task<ICollection<Address>> GetAddressesByCompanyIdAsync(Guid companyId)
    {
        try
        {
            return await _dbContext.Addresses
                                   .Where(a => a.CompanyId == companyId)  // Correct comparison with Guid
                                   .ToListAsync();
        }
        catch (Exception ex)
        {
            // Log with companyId masked
            _logger.LogError("Error retrieving addresses for company (ID masked). Error details: {ErrorMessage}",
                $"{companyId.ToString().Substring(0, 5)}****{companyId.ToString().Substring(companyId.ToString().Length - 4)}");
            throw;
        }
    }

    // Update an existing address
    public async Task UpdateAsync(Address address)
    {
        try
        {
            // If updating to Primary address, ensure no other primary address exists
            if (address.AddressType == "Primary")
            {
                bool hasPrimaryAddress = await _dbContext.Addresses
                    .AnyAsync(a => a.CompanyId == address.CompanyId && a.AddressType == "Primary" && a.Id != address.Id);

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Företaget har redan en primäradress.");  // "The company already has a primary address."
                }
            }

            _dbContext.Addresses.Update(address);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the error with a generic message, excluding sensitive data like company ID
            _logger.LogError("Error updating address. Error details: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
