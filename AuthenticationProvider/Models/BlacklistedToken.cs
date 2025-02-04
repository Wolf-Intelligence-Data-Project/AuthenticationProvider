using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class BlacklistedToken
{
    [Required]
    public string Token { get; set; } = string.Empty;
    [Required]
    public DateTime ExpirationTime { get; set; }
}
