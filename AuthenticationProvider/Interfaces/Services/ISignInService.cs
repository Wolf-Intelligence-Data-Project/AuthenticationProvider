using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Data.Dtos;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface ISignInService
{
    Task<SignInResponse> SignInAsync(SignInDto signInDto);
}
