using AuthenticationProvider.Data.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface IAccountVerificationTokenService
{
    Task<string> CreateAccountVerificationTokenAsync(Guid companyId);

    Task<ClaimsPrincipal> ValidateAccountVerificationTokenAsync(string token);

    Task MarkAccountVerificationTokenAsUsedAsync(string token);

    Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string token);

    Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId);
}
