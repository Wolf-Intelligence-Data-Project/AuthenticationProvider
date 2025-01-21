using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Models.Responses;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface ISignUpService
{
    Task<SignUpResponse> RegisterCompanyAsync(SignUpDto request);
    Task DeleteCompanyAsync(Guid companyId);
}
