namespace AuthenticationProvider.Interfaces;

public interface IAccountVerificationTokenService
{
    Task<string> CreateAccountVerificationTokenAsync(Guid companyId);
    Task<bool> SendVerificationEmailAsync(string token, string email);
    Task<bool> ValidateTokenAsync(string token);
    Task MarkTokenAsUsedAsync(string token);
}