using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class SendVerificationRequest
{
    [Required]
    public string VerificationId { get; set; }
}
