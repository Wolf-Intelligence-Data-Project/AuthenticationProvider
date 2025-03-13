using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddAsync(AddressEntity address);
    Task<AddressEntity> GetByIdAsync(Guid addressId);
    Task<IEnumerable<AddressEntity>> GetAddressesByUserIdAsync(Guid userId);
    Task UpdateAsync(AddressEntity address);
}
