using AuthenticationProvider.Models;
using AuthenticationProvider.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationProvider.Data;

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
    public async Task<ICollection<Address>> GetAddressesByCompanyIdAsync(int companyId)
    {
        return await _dbContext.Addresses
                               .Where(a => a.CompanyId == companyId)
                               .ToListAsync();
    }

    // Update an existing address
    public async Task UpdateAsync(Address address)
    {
        _dbContext.Addresses.Update(address);
        await _dbContext.SaveChangesAsync();
    }

    // Delete an address by its ID
    public async Task DeleteAsync(int id)
    {
        var address = await _dbContext.Addresses.FindAsync(id);
        if (address != null)
        {
            _dbContext.Addresses.Remove(address);
            await _dbContext.SaveChangesAsync();
        }
    }
}
