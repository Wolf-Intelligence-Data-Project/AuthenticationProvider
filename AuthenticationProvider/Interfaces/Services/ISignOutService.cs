namespace AuthenticationProvider.Interfaces.Services;

public interface ISignOutService
{
    Task<bool> SignOutAsync(string token);
}
