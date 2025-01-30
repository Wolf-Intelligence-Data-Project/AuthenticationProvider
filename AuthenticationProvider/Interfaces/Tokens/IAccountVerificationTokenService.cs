using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Tokens;

public interface IAccountVerificationTokenService
{
    Task<string> CreateAccountVerificationTokenAsync(Guid companyId);

    Task<IActionResult> ValidateAccountVerificationTokenAsync(TokenRequest request);

    Task MarkAccountVerificationTokenAsUsedAsync(string token);

    Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string token);

    Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId);
}
