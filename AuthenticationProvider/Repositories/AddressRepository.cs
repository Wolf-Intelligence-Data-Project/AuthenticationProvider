using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;

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

    /// <summary>
    /// Adds a new address to the database with uniqueness checks for the same company.
    /// </summary>
    /// <param name="address">The address entity to add.</param>
    public async Task AddAsync(AddressEntity address)
    {
        try
        {
            // Ensure address uniqueness: Same street address, postal code, and city should not exist for the same company
            bool isAddressUnique = !await _dbContext.Addresses
                .AnyAsync(a => a.StreetAddress == address.StreetAddress
                               && a.PostalCode == address.PostalCode
                               && a.City == address.City
                               && a.CompanyId == address.CompanyId);

            if (!isAddressUnique)
            {
                throw new InvalidOperationException("Adressen existerar redan i systemet.");
            }

            // If adding a primary address, check if the company already has a primary address
            if (address.IsPrimary)
            {
                bool hasPrimaryAddress = await _dbContext.Addresses
                    .AnyAsync(a => a.CompanyId == address.CompanyId && a.IsPrimary);

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Företaget har redan en primäradress.");
                }
            }

            // Add the address to the database
            await _dbContext.AddAsync(address);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding address.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves an address by its ID.
    /// </summary>
    /// <param name="id">The ID of the address to retrieve.</param>
    /// <returns>The address entity if found, otherwise null.</returns>
    public async Task<AddressEntity> GetByIdAsync(int id)
    {
        try
        {
            return await _dbContext.Addresses
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving the address");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all addresses for a specific company.
    /// </summary>
    /// <param name="companyId">The ID of the company whose addresses to retrieve.</param>
    /// <returns>A collection of address entities.</returns>
    public async Task<IEnumerable<AddressEntity>> GetAddressesByCompanyIdAsync(Guid companyId)
    {
        try
        {
            return await _dbContext.Addresses
                                    .Where(a => a.CompanyId == companyId)
                                    .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses.");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing address in the database.
    /// </summary>
    /// <param name="address">The updated address entity.</param>
    public async Task UpdateAsync(AddressEntity address)
    {
        try
        {
            // If updating to Primary address, ensure no other primary address exists
            if (address.IsPrimary)
            {
                bool hasPrimaryAddress = await _dbContext.Addresses
                    .AnyAsync(a => a.CompanyId == address.CompanyId && a.IsPrimary && a.Id != address.Id);

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Företaget har redan en primäradress.");
                }
            }

            _dbContext.Addresses.Update(address);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address.");
            throw;
        }
    }
}
