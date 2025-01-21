using AuthenticationProvider.Data;
using AuthenticationProvider.Data.Entities;

namespace AuthenticationProvider.Interfaces.Services;

public interface IAccessTokenService
{
    string GenerateAccessToken(ApplicationUser user);
    void RevokeAccessToken(string userId);
    string GetUserIdFromToken(string token);
    bool IsTokenValid(string token);
}
