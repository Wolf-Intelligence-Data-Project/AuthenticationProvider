using AuthenticationProvider.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Entities;

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
    public bool TermsAndConditions { get; set; } // Consent to terms

    [Required]
    public int PrimaryAddressId { get; set; }

    [ForeignKey("PrimaryAddressId")]
    public Address PrimaryAddress { get; set; } = new Address();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Tokens for email verification and login session
    public string LastEmailVerificationToken { get; set; } = string.Empty;
    public string LastLoginSessionToken { get; set; } = string.Empty; // To store the last login session token
}
