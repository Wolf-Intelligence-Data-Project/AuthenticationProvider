namespace AuthenticationProvider.Interfaces;

public interface ITokenRevocationService
{
    Task<bool> RevokeTokenAsync(string token);
}