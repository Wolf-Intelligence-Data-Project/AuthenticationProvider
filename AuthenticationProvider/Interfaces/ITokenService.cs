using System.Security.Claims;

namespace AuthenticationProvider.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string userName);
    ClaimsPrincipal ValidateToken(string token);
}