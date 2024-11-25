namespace AuthenticationProvider.Interfaces;

public interface ISignOutService
{
    Task<bool> SignOutAsync(string token);
}
