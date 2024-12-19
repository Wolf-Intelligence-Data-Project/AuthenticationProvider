using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Data;
using System.Linq;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;

namespace AuthenticationProvider.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AddressRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Add a new address to the database
    public async Task AddAsync(AddressEntity address)
    {
        // Ensure address uniqueness: Same street address, postal code, and city should not exist for the same company
        bool isAddressUnique = !await _dbContext.Addresses
            .AnyAsync(a => a.StreetAddress == address.StreetAddress
                           && a.PostalCode == address.PostalCode
                           && a.City == address.City
                           && a.CompanyId == address.CompanyId); // Ensure uniqueness per company

        if (!isAddressUnique)
        {
            throw new InvalidOperationException("Adressen existerar redan i systemet.");  // "The address already exists in the system."
        }

        // If adding a primary address, check if the company already has a primary address
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
    public async Task<AddressEntity> GetByIdAsync(int id)
    {
        return await _dbContext.Addresses
                               .AsNoTracking()
                               .FirstOrDefaultAsync(a => a.Id == id);
    }

    // Get all addresses for a specific company
    public async Task<ICollection<AddressEntity>> GetAddressesByCompanyIdAsync(Guid companyId)  // Update to use Guid for companyId
    {
        return await _dbContext.Addresses
                               .Where(a => a.CompanyId == companyId)  // Correct comparison with Guid
                               .ToListAsync();
    }

    // Update an existing address
    public async Task UpdateAsync(AddressEntity address)
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
