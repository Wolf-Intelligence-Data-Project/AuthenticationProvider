using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Services.Tokens;

public interface IResetPasswordTokenService
{
    Task<TokenInfoModel> GenerateResetPasswordTokenAsync(string email);

    Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string tokenId);

    Task<bool> ValidateResetPasswordTokenAsync(string token);

    Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);

}
