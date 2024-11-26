using AuthenticationProvider.Interfaces;
using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Data;
using AuthenticationProvider.Models;
using System.Linq;

namespace AuthenticationProvider.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AddressRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Add a new address to the database
    public async Task AddAsync(Address address)
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

    // Get an address by its ID
    public async Task<Address> GetByIdAsync(int id)
    {
        return await _dbContext.Addresses
                               .AsNoTracking()
                               .FirstOrDefaultAsync(a => a.Id == id);
    }

    // Get all addresses for a specific company
    public async Task<ICollection<Address>> GetAddressesByCompanyIdAsync(Guid companyId)  // Update to use Guid for companyId
    {
        return await _dbContext.Addresses
                               .Where(a => a.CompanyId == companyId)  // Correct comparison with Guid
                               .ToListAsync();
    }

    // Update an existing address
    public async Task UpdateAsync(Address address)
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
}
