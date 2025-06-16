using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Requests;

public class ResetPasswordRequest
{
    [Required]
    public string ResetId { get; set; }

    [Required(ErrorMessage = "Lösenord krävs.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$",
     ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Bekräfta lösenord krävs.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; }
}