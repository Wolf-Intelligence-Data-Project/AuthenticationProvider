using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class EmailDto
{
    [Required]
    public string Email { get; set; }
}