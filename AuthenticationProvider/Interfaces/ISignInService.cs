using System.Threading.Tasks;
using AuthenticationProvider.Models.SignIn;

namespace AuthenticationProvider.Interfaces
{
    public interface ISignInService
    {
        Task<SignInResponse> SignInAsync(SignInRequest request);
    }
}
