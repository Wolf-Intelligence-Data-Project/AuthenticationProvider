namespace AuthenticationProvider.Interfaces;

public interface IResetPasswordService
{
    Task<bool> SendResetPasswordEmailAsync(string token);
}
