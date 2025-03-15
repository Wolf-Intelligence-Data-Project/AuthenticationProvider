using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IAccountVerificationTokenRepository
{
    Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token);

    Task<AccountVerificationTokenEntity> GetByTokenAsync(string token);

    Task<AccountVerificationTokenEntity> GetByIdAsync(Guid tokenId);

    Task MarkAsUsedAsync(Guid tokenId);

    Task RevokeAndDeleteAsync(Guid userId);

    Task RevokeAndDeleteByTokenAsync(string token);
}
