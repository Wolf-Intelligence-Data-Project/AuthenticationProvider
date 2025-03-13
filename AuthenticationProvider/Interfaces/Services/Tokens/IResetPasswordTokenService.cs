using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Services.Tokens;

public interface IResetPasswordTokenService
{
    Task<object> GenerateResetPasswordTokenAsync(string email);

    Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token);

    Task<bool> ValidateResetPasswordTokenAsync(string token);

    Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);

}
