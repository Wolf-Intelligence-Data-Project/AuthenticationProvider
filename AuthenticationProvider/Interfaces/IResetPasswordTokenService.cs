using AuthenticationProvider.Models;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces
{
    public interface IResetPasswordTokenService
    {
        Task<string> CreateResetPasswordTokenAsync(Guid companyId);
        Task<ResetPasswordToken> GetValidResetPasswordTokenAsync(string token);
        Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);
        Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId);
        Task<bool> ResetCompanyPasswordAsync(string email, string newPassword);
    }
}
