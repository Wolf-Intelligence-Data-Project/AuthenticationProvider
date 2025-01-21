using AuthenticationProvider.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services
{
    public interface IResetPasswordTokenService
    {
        Task<string> CreateResetPasswordTokenAsync(string email);
        Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token);
        Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);
        Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId);
        Task<bool> ResetCompanyPasswordAsync(string email, string newPassword);

        Task<bool> ValidateResetPasswordTokenAsync(string token);
        Task<string> GetEmailFromTokenAsync(string token);
    }
}
