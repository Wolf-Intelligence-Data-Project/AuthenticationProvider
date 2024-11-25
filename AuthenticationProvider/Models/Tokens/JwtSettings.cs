namespace AuthenticationProvider.Models.Tokens;

public class JwtSettings
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
}
