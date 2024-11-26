using AuthenticationProvider.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAddressRepository
{
    // Add a new address to the database
    Task AddAsync(Address address);

    // Get an address by its ID
    Task<Address> GetByIdAsync(int id);

    // Get all addresses for a specific company (Updated to use Guid for companyId)
    Task<ICollection<Address>> GetAddressesByCompanyIdAsync(Guid companyId);

    // Update an existing address
    Task UpdateAsync(Address address);
}
