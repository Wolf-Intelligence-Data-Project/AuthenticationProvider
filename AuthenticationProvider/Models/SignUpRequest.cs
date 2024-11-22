using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class SignUpRequest
{
    // Organisation Number (Autopopulated or manually entered)
    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisation number must be 10 digits.")]
    public string OrganisationNumber { get; set; } = null!;

    // Company Name
    [Required]
    public string CompanyName { get; set; } = null!;

    // Company Email
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    // Business Type
    [Required]
    public string BusinessType { get; set; } = null!;

    // Responsible Person's Name (e.g., CEO)
    [Required]
    public string ResponsiblePersonName { get; set; } = null!;

    // Phone Number
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = null!;

    // Consent for Terms and Conditions
    [Required]
    public bool TermsAndConditions { get; set; }

    // Minimal address details
    [Required]
    public string StreetAddress { get; set; } = null!;

    [Required]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Postal code must be 5 digits.")]
    public string PostalCode { get; set; } = null!;

    [Required]
    public string City { get; set; } = null!;

    public string? Region { get; set; }

    // Password for authentication
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    // Confirm Password for verification
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}
