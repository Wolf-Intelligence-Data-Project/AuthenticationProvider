using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class SignInRequest
{
    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    public string? CaptchaToken { get; set; }
}

