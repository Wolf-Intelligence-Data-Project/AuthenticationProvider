namespace AuthenticationProvider.Models.Tokens
{
    public enum VerificationResult
    {
        Success,
        InvalidToken,
        EmailNotFound,
        CompanyNotFound,
        AlreadyVerified
    }
}
