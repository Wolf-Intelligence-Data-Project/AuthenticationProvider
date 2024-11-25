using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.SignUp;

public class SignUpRequest
{
    // Organisation Number (Autopopulated or manually entered)
    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisation number must be 10 digits.")]
    public string OrganisationNumber { get; set; } = string.Empty;

    // Company Name
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    // Company Email
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Business Type
    [Required]
    public string BusinessType { get; set; } = string.Empty;

    // Responsible Person's Name (e.g., CEO)
    [Required]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    // Phone Number
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    // Consent for Terms and Conditions
    [Required]
    public bool TermsAndConditions { get; set; }

    // Password for authentication
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Confirm Password for verification
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Primary address reference (if necessary)
    // Optional: You can include a reference to the primary address ID if you wish.
    public int? PrimaryAddressId { get; set; }
}
