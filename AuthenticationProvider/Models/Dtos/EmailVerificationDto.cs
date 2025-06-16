using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Dtos;

public class EmailVerificationDto
{
    [Required]
    public string VerificationId { get; set; }
}
