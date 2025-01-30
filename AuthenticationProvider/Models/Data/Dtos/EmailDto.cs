using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Dtos;

public class EmailDto
{
    [Required]
    public string Email { get; set; }
}