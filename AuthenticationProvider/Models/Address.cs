using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class Address
{
    public int Id { get; set; }

    // Street address
    [Required]
    public string StreetAddress { get; set; } = null!;

    // Postal code (Postnummer)
    [Required]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Postal code must be 5 digits.")]
    public string PostalCode { get; set; } = null!;

    // City (Stad)
    [Required]
    public string City { get; set; } = null!;

    // Region (if applicable, e.g., County for Sweden)
    public string? Region { get; set; }

    // Country (could be a dropdown)
    [Required]
    public string Country { get; set; } = "Sweden";

    // Reference back to the company (Many addresses can belong to one company)
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!; // Navigation property to Company

    // Additional fields to categorize the address ("Head Office", "Branch Office")
    public string? AddressType { get; set; }
}