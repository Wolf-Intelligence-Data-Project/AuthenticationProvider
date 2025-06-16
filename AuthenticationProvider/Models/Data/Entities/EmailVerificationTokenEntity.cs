using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Models.Data.Entities;

public class EmailVerificationTokenEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("UserEntity")]
    public Guid UserId { get; set; }

    public virtual UserEntity User { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required]
    public bool IsUsed { get; set; } = false;
}
