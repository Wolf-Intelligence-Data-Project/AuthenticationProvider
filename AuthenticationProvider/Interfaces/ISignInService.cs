using System.Threading.Tasks;
using AuthenticationProvider.Models;

namespace AuthenticationProvider.Interfaces
{
    public interface ISignInService
    {
        Task<SignInResponse> SignInAsync(SignInRequest request);
    }
}
