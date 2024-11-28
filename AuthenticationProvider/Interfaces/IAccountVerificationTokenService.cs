using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationTokenService
{
    Task<string> GenerateVerificationTokenAsync(Guid companyId);
    Task<bool> RevokeVerificationTokenAsync(Guid companyId);
    Task<bool> IsVerificationTokenRevokedAsync(Guid companyId);
    Task<bool> IsVerificationTokenExpiredAsync(string token);
}
