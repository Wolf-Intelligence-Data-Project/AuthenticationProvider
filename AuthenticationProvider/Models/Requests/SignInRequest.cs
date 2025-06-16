using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Requests;

public class SignInRequest
{
    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public string? CaptchaToken { get; set; }
}

