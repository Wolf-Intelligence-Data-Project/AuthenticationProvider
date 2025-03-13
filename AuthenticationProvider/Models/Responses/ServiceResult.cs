namespace AuthenticationProvider.Models.Responses;

public enum ServiceResult
{
    Success,
    InvalidToken,
    EmailNotFound,
    UserNotFound,
    AlreadyVerified,
    Failure
}
