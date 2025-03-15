using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Models.Data.Entities;

public class AccountVerificationTokenEntity
{
    [Key]
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public DateTime ExpiryDate { get; set; }
    [Required]
    public bool IsUsed { get; set; } = false;

    [ForeignKey("UserId")] // Foreign key linking to UserEntity
    [Required]
    public UserEntity User { get; set; } = null!;
}
