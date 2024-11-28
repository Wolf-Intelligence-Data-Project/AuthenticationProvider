using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface ILoginSessionTokenRepository
{
    Task<string> GetLoginSessionTokenAsync(Guid companyId);
    Task<bool> UpdateLoginSessionTokenAsync(Guid companyId, string token);
    Task SaveLoginSessionTokenAsync(Guid companyId, string token);

}
