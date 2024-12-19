using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Dtos;

public class SignInDto
{
    // Email address for the company
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ogiltigt e-postformat.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte överstiga 256 tecken.")]
    public string Email { get; set; } = null!;

    // Password for authentication
    [Required(ErrorMessage = "Lösenord krävs.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$",
        ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string Password { get; set; } = null!;
}
