namespace AuthenticationProvider.Models.Responses;

public enum TokenValidationResult
{
    Valid,
    Expired,
    Invalid,
    Blacklisted,
    MissingToken
}
