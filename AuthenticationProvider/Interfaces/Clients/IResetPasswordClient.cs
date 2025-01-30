namespace AuthenticationProvider.Interfaces.Clients;

public interface IResetPasswordClient
{
    Task<bool> SendResetPasswordEmailAsync(string token);
}
