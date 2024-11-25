using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.SignIn;

public class SignInRequest
{
    // Email address for the company
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    // Password for authentication
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}
