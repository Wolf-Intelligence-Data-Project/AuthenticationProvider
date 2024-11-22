using AuthenticationProvider.Models;

namespace AuthenticationProvider.Interfaces;

public interface ISignUpService
{
    Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request);
}
