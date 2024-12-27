using AuthenticationProvider.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces
{
    public interface IResetPasswordTokenService
    {
        Task<string> CreateResetPasswordTokenAsync(string Email);
        Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token);
        Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);
        Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId);
        Task<bool> ResetCompanyPasswordAsync(string email, string newPassword);

        // Add the GetEmailFromTokenAsync method signature
        Task<string> GetEmailFromTokenAsync(string token);
    }
}
