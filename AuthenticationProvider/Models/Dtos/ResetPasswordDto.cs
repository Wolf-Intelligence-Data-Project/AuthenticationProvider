using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Dtos;

public class ResetPasswordDto
{
    [Required]
    public string ResetId { get; set; }
}
