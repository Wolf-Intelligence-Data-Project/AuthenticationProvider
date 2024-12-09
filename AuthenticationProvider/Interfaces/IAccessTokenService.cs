using Microsoft.AspNetCore.Identity;

namespace AuthenticationProvider.Interfaces;

public interface IAccessTokenService
{
    string GenerateAccessToken(IdentityUser user);
    void RevokeAccessToken(string userId);
    string GetToken(string userId);
}
