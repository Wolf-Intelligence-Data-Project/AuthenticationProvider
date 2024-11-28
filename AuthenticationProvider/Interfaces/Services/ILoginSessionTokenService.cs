using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services;

public interface ILoginSessionTokenService
{
    Task<string> GenerateLoginSessionTokenAsync(string email);
    Task<bool> InvalidateLoginSessionTokenAsync(string email);
    Task<bool> IsLoginSessionTokenExpiredAsync(string email);
    Task<bool> RevokeLoginSessionTokenAsync(string email);
}
