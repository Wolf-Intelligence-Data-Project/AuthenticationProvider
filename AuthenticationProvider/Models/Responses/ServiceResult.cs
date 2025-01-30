namespace AuthenticationProvider.Models.Responses;

public enum ServiceResult
{
    Success,
    InvalidToken,
    EmailNotFound,
    CompanyNotFound,
    AlreadyVerified,
    Failure
}
