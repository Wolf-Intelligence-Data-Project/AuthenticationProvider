namespace AuthenticationProvider.Models;

public class SignUpResponse
{
    public bool Success { get; set; }
    public string UserId { get; set; }
    public int CompanyId { get; set; }
    public string Token { get; set; }
}
