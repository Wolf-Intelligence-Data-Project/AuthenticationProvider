using AuthenticationProvider.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IAddressRepository
{
    // Add a new address to the database
    Task AddAsync(AddressEntity address);

    // Get an address by its ID
    Task<AddressEntity> GetByIdAsync(int id);

    // Get all addresses for a specific company (Updated to use Guid for companyId)
    Task<IEnumerable<AddressEntity>> GetAddressesByCompanyIdAsync(Guid companyId);

    // Update an existing address
    Task UpdateAsync(AddressEntity address);
}
