namespace AuthenticationProvider.Interfaces.Services;

public interface IResetPasswordService
{
    Task<bool> SendResetPasswordEmailAsync(string token);
}
