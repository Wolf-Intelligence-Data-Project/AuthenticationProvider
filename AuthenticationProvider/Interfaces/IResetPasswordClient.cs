namespace AuthenticationProvider.Interfaces;

public interface IResetPasswordClient
{
    Task<bool> SendResetPasswordEmailAsync(string token);
}
