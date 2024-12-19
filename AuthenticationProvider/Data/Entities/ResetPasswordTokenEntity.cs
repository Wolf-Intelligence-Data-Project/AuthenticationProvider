namespace AuthenticationProvider.Data.Entities;

public class ResetPasswordTokenEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;

    // Navigation property to Company
    public CompanyEntity Company { get; set; }
}
