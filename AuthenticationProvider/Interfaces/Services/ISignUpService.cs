using AuthenticationProvider.Models.SignUp;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface ISignUpService
{
    // Registers a new company and sends the verification email
    Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request);
}
