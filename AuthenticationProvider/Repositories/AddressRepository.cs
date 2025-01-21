using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Data;
using System.Linq;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using System.Threading.Tasks;

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
        try
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
            if (address.IsPrimary)
            {
                bool hasPrimaryAddress = await _dbContext.Addresses
                    .AnyAsync(a => a.CompanyId == address.CompanyId && a.IsPrimary);

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
            // Log the exception (example log)
            Console.WriteLine($"Error adding address: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Get an address by its ID
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
            // Log the exception
            Console.WriteLine($"Error retrieving address by ID: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Get all addresses for a specific company
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
            // Log the exception
            Console.WriteLine($"Error retrieving addresses by company ID: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }

    // Update an existing address
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
                    throw new InvalidOperationException("Företaget har redan en primäradress.");  // "The company already has a primary address."
                }
            }

            _dbContext.Addresses.Update(address);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error updating address: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            throw;  // Re-throw the exception to handle it elsewhere
        }
    }
}
