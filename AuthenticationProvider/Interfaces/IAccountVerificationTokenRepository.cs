using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationTokenRepository
{
    Task<string> GetLastEmailVerificationTokenAsync(Guid companyId);
    Task<bool> UpdateLastEmailVerificationTokenAsync(Guid companyId, string token);
    Task SaveEmailVerificationTokenAsync(Guid companyId, string token);
}
