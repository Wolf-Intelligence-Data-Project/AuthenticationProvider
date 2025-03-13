using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Entities;

public class AccountVerificationTokenEntity
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public DateTime ExpiryDate { get; set; }
    [Required]
    public bool IsUsed { get; set; } = false;
    [Required]
    public UserEntity User { get; set; }
}
