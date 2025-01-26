using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Entities;

public class ResetPasswordTokenEntity
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public Guid CompanyId { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public DateTime ExpiryDate { get; set; }
    [Required]
    public bool IsUsed { get; set; } = false;

    [Required]
    public CompanyEntity Company { get; set; }
}
