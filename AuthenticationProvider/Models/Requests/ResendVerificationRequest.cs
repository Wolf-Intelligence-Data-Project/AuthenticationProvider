using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Requests;

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "E-postadress krävs.")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    public string Email { get; set; }
}