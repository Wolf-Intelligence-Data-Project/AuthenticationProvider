using AuthenticationProvider.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddAsync(AddressEntity address);
    Task<AddressEntity> GetByIdAsync(int id);
    Task<IEnumerable<AddressEntity>> GetAddressesByCompanyIdAsync(Guid companyId);
    Task UpdateAsync(AddressEntity address);
}
