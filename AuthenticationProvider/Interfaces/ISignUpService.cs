using AuthenticationProvider.Models.SignUp;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface ISignUpService
{
    Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request);
    Task DeleteCompanyAsync(Guid companyId);
}
