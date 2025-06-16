using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationTokenEntity> CreateAsync(EmailVerificationTokenEntity token);

    Task<EmailVerificationTokenEntity> GetByTokenAsync(string token);

    Task<EmailVerificationTokenEntity> GetByIdAsync(Guid tokenId);

    Task MarkAsUsedAsync(Guid tokenId);

    Task RevokeAndDeleteAsync(Guid userId);

    Task RevokeAndDeleteByTokenAsync(string token);
}
