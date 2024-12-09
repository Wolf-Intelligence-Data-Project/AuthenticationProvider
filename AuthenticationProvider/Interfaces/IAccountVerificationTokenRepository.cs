using AuthenticationProvider.Models;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces
{
    public interface IAccountVerificationTokenRepository
    {
        Task<AccountVerificationToken> CreateAsync(AccountVerificationToken token);
        Task<AccountVerificationToken> GetByTokenAsync(string token);
        Task MarkAsUsedAsync(Guid tokenId);
        Task DeleteAsync(Guid companyId);
    }
}
