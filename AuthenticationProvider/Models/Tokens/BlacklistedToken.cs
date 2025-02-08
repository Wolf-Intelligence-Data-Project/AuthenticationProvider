namespace AuthenticationProvider.Models.Tokens;

public class BlacklistedToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpirationTime { get; set; }
}
