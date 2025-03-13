using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IResetPasswordTokenRepository
{
    Task<ResetPasswordTokenEntity> GetByIdAsync(Guid id);
    Task<ResetPasswordTokenEntity> GetByTokenAsync(string token);
    Task<ResetPasswordTokenEntity> CreateAsync(ResetPasswordTokenEntity token);
    Task DeleteAsync(Guid userId);
    Task MarkAsUsedAsync(Guid tokenId);
}
