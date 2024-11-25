using AuthenticationProvider.Models.SignUp;

namespace AuthenticationProvider.Interfaces;

public interface ISignUpService
{
    Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request);
}
