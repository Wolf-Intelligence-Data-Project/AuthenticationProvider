namespace AuthenticationProvider.Models.Responses;

public enum VerificationResult
{
    Success,
    InvalidToken,
    EmailNotFound,
    CompanyNotFound,
    AlreadyVerified
}
