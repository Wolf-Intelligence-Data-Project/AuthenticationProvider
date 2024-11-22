using AuthenticationProvider.Models;

namespace AuthenticationProvider.Interfaces;

public interface ITokenProvider
{
    Task<string> GenerateTokenAsync(string email, TokenType tokenType);
}
