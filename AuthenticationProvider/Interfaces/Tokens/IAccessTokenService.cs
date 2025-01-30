using AuthenticationProvider.Models.Data;

namespace AuthenticationProvider.Interfaces.Tokens;

public interface IAccessTokenService
{
    string GenerateAccessToken(ApplicationUser user);
    void RevokeAccessToken(string userId);
    string GetUserIdFromToken(string token);
    bool IsTokenValid(string token);
}
