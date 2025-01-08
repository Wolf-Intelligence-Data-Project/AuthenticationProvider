namespace AuthenticationProvider.Models.Responses;

public class SignUpResponse
{
    public bool Success { get; set; }
    public string UserId { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public string Token { get; set; } = null!;
    public string? ErrorMessage { get; set; }
}
