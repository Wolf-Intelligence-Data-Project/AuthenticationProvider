using AuthenticationProvider.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Repositories
{
    public interface IAccountVerificationTokenRepository
    {
        // Method to create a token
        Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token);

        // Method to retrieve a token by its string representation
        Task<AccountVerificationTokenEntity> GetByTokenAsync(string token);

        // Method to mark a token as used
        Task MarkAsUsedAsync(Guid tokenId);

        // New method to revoke and delete tokens for a specific company
        Task RevokeAndDeleteAsync(Guid companyId);
    }
}
