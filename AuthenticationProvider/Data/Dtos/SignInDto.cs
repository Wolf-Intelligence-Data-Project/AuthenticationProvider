using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Dtos;

public class SignInDto
{
    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Ogiltiga inloggningsuppgifter.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}

