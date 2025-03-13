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
    private readonly UserDbContext _userDbContext;
    private readonly ILogger<AddressRepository> _logger;

    public AddressRepository(UserDbContext dbContext, ILogger<AddressRepository> logger)
    {
        _userDbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Adds a new address to the database with uniqueness checks for the same user.
    /// </summary>
    /// <param name="address">The address entity to add.</param>
    public async Task AddAsync(AddressEntity address)
    {
        try
        {
            // Ensure address uniqueness: Same street address, postal code, and city should not exist for the same user
            bool isAddressUnique = !await _userDbContext.Addresses
                .AnyAsync(a => a.StreetAndNumber == address.StreetAndNumber
                               && a.PostalCode == address.PostalCode
                               && a.City == address.City
                               && a.UserId == address.UserId);

            if (!isAddressUnique)
            {
                throw new InvalidOperationException("Adressen existerar redan i systemet.");
            }

            // If adding a primary address, check if the user already has a primary address
            if (address.IsPrimary)
            {
                bool hasPrimaryAddress = await _userDbContext.Addresses
                    .AnyAsync(a => a.UserId == address.UserId && a.IsPrimary);

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Användaren har redan en primäradress.");
                }
            }

            // Add the address to the database
            await _userDbContext.AddAsync(address);
            await _userDbContext.SaveChangesAsync();
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
    /// <param name="addressId">The ID of the address to retrieve.</param>
    /// <returns>The address entity if found, otherwise null.</returns>
    public async Task<AddressEntity> GetByIdAsync(Guid addressId)
    {
        try
        {
            return await _userDbContext.Addresses
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(a => a.AddressId == addressId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving the address");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all addresses for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose addresses to retrieve.</param>
    /// <returns>A collection of address entities.</returns>
    public async Task<IEnumerable<AddressEntity>> GetAddressesByUserIdAsync(Guid userId)
    {
        try
        {
            return await _userDbContext.Addresses
                                    .Where(a => a.UserId == userId)
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
                bool hasPrimaryAddress = await _userDbContext.Addresses
                    .AnyAsync(a => a.UserId == address.UserId && a.IsPrimary && a.AddressId != address.AddressId);

                if (hasPrimaryAddress)
                {
                    throw new InvalidOperationException("Användaren har redan en primäradress.");
                }
            }

            _userDbContext.Addresses.Update(address);
            await _userDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address.");
            throw;
        }
    }
}
