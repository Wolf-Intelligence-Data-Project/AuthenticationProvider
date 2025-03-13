using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IUserRepository
{
    Task<bool> UserExistsAsync(string identificationNumber, string email);
    Task AddAsync(UserEntity user);
    Task UpdateAsync(UserEntity user);
    Task<UserEntity> GetByEmailAsync(string email);
    Task<UserEntity> GetByIdAsync(Guid userId);
    Task DeleteAsync(Guid userId);
}