using AuthenticationProvider.Models;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces
{
    public interface IAccountVerificationTokenRepository
    {
        // Method to create a token
        Task<AccountVerificationToken> CreateAsync(AccountVerificationToken token);

        // Method to retrieve a token by its string representation
        Task<AccountVerificationToken> GetByTokenAsync(string token);

        // Method to mark a token as used
        Task MarkAsUsedAsync(Guid tokenId);

        // New method to revoke and delete tokens for a specific company
        Task RevokeAndDeleteAsync(Guid companyId);
    }
}
