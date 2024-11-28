using System.Threading.Tasks;
using AuthenticationProvider.Models.SignIn;

namespace AuthenticationProvider.Interfaces.Services;

public interface ISignInService
{
    Task<SignInResponse> SignInAsync(SignInRequest request);
}
