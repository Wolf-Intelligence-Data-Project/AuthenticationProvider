using AuthenticationProvider.Models.Responses;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services.Security;

public interface IAccountVerificationService
{
    Task<ServiceResult> SendVerificationEmailAsync(string token);
    Task<ServiceResult> VerifyEmailAsync(string token);
}
