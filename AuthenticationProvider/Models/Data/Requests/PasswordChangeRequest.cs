using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class PasswordChangeRequest
{
    [Required(ErrorMessage = "Lösenord krävs.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$",
       ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Bekräfta lösenord krävs.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;
}
