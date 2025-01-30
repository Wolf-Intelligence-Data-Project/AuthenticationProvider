using AuthenticationProvider.Models.Responses;
using System.Threading.Tasks;
using AuthenticationProvider.Models.Data.Dtos;

namespace AuthenticationProvider.Interfaces.Services;

public interface ISignInService
{
    Task<SignInResponse> SignInAsync(SignInDto signInDto);
}
