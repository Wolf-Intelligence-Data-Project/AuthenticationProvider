using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class TokenRequest
{
    [Required]
    public string Token { get; set; }
}
