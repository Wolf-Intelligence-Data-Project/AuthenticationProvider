using AuthenticationProvider.Models.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Tokens;

public interface IResetPasswordTokenService
{
    Task<string> CreateResetPasswordTokenAsync(string email);

    Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token);

    Task<string> GetEmailFromTokenAsync(string token);

    Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);

    Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId);

    Task<bool> ValidateResetPasswordTokenAsync(string token);
}
