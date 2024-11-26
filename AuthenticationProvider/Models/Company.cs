using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class Company
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string OrganisationNumber { get; set; } = string.Empty;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public BusinessType BusinessType { get; set; } = BusinessType.Unspecified;

    [Required]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;

    [Required]
    public bool TermsAndConditions { get; set; }

    [Required]
    public int PrimaryAddressId { get; set; }

    [ForeignKey("PrimaryAddressId")]
    public Address PrimaryAddress { get; set; } = new Address();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    // This is where you store the hashed password
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
}