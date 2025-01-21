using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; }
    [Required]
    public string NewPassword { get; set; }
    [Required]
    public string ConfirmPassword { get; set; }
}