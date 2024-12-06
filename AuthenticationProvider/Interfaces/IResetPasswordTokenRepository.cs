using AuthenticationProvider.Models;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories
{
    public interface IResetPasswordTokenRepository
    {
        Task<ResetPasswordToken> GetByTokenAsync(string token);
        Task<ResetPasswordToken> CreateAsync(ResetPasswordToken token);
        Task DeleteAsync(Guid companyId);
        Task MarkAsUsedAsync(Guid tokenId);
    }
}
