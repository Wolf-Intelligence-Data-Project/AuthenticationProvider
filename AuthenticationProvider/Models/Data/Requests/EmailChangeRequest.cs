using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class EmailChangeRequest
{

    [Required(ErrorMessage = "Det nuvarande lösenordet är inte giltigt.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "E-postadressen är ogiltig.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte vara längre än 256 tecken.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "E-postadressen följer inte standardformatet.")]
    public string Email { get; set; } = null!;

}
