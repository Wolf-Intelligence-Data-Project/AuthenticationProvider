namespace AuthenticationProvider.Services;

public interface IEmailRestrictionService
{
    bool IsRestrictedEmail(string email);
}