namespace AuthenticationProvider.Models.Responses;

public class SignUpResponse
{
    public bool Success { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    public string? ErrorMessage { get; set; }
}
