using AuthenticationProvider.Data.Entities;
using Microsoft.Extensions.Primitives;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IAccountVerificationTokenRepository
{
    Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token);

    Task<AccountVerificationTokenEntity> GetByTokenAsync(string token);

    Task<AccountVerificationTokenEntity> GetTokenByIdAsync(Guid tokenId);

    Task MarkAsUsedAsync(Guid tokenId);

    Task RevokeAndDeleteAsync(Guid companyId);

    Task RevokeAndDeleteByTokenAsync(string token);
}
