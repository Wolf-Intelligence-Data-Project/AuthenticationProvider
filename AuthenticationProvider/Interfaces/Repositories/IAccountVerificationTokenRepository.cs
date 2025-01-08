using AuthenticationProvider.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories
{
    public interface IAccountVerificationTokenRepository
    {
        Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token);

        Task<AccountVerificationTokenEntity> GetByTokenAsync(string token);

        Task<AccountVerificationTokenEntity> GetTokenByIdAsync(Guid tokenId); // New method

        Task MarkAsUsedAsync(Guid tokenId);

        Task RevokeAndDeleteAsync(Guid companyId);
    }
}
