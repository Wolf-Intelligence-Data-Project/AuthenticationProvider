using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class Company
{
    public int Id { get; set; }

    [Required]
    public string OrganisationNumber { get; set; } = string.Empty;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public BusinessType BusinessType { get; set; } = BusinessType.Unspecified;  // Default value for enum

    [Required]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;

    [Required]
    public bool TermsAndConditions { get; set; }

    [Required]
    public int PrimaryAddressId { get; set; }  

    [Required]
    public Address PrimaryAddress { get; set; } = new Address(); // Ensures PrimaryAddress is initialized

    public ICollection<Address> Addresses { get; set; } = new List<Address>();  // Initializes the collection to avoid null reference
}
