using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class DeleteRequest
{
    [Required(ErrorMessage = "Ogiltigt lösenord.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}
