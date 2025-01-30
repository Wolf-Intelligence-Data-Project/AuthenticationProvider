using AuthenticationProvider.Models.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface IResetPasswordTokenRepository
{
    Task<ResetPasswordTokenEntity> GetByIdAsync(Guid id);
    Task<ResetPasswordTokenEntity> GetByTokenAsync(string token);
    Task<ResetPasswordTokenEntity> CreateAsync(ResetPasswordTokenEntity token);
    Task DeleteAsync(Guid companyId);
    Task MarkAsUsedAsync(Guid tokenId);
}
