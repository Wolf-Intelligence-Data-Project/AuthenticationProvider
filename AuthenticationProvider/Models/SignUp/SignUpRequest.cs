using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.SignUp;

public class SignUpRequest
{
    // Organisation Number (Autopopulated or manually entered) - Swedish organisation number format (10 digits)
    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisationsnummer måste vara exakt 10 siffror.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Organisationsnummer får endast innehålla siffror.")]
    public string OrganisationNumber { get; set; } = string.Empty;

    // Company Name
    [Required]
    [StringLength(100, ErrorMessage = "Företagsnamnet får inte överstiga 100 tecken.")]
    public string CompanyName { get; set; } = string.Empty;

    // Company Email
    [Required]
    [EmailAddress(ErrorMessage = "Ogiltigt e-postformat.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte överstiga 256 tecken.")]
    public string Email { get; set; } = string.Empty;

    // Business Type
    [Required]
    [EnumDataType(typeof(BusinessType), ErrorMessage = "Ogiltig företagstyp.")]
    public BusinessType BusinessType { get; set; }

    // Responsible Person's Name (e.g., CEO)
    [Required]
    [StringLength(100, ErrorMessage = "Ansvarig persons namn får inte överstiga 100 tecken.")]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    // Phone Number - Swedish format (e.g., +46 for Sweden)
    [Required]
    [Phone]
    [RegularExpression(@"^\+46\d{9}$", ErrorMessage = "Telefonnumret måste vara ett giltigt svenskt nummer som börjar med +46.")]
    public string PhoneNumber { get; set; } = string.Empty;

    // Consent for Terms and Conditions
    [Required]
    public bool TermsAndConditions { get; set; }

    // Password for authentication - Swedish websites often require strong passwords
    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$", ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string Password { get; set; } = string.Empty;

    // Confirm Password for verification
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Primary address reference (required) - address validation may be handled separately
    [Required(ErrorMessage = "Primär adress är obligatorisk.")]
    public int PrimaryAddressId { get; set; }  // Make it required for registration

    // Custom validation method for email uniqueness or additional business logic
    public bool IsEmailValid(string email)
    {
        // Custom email validation if needed (e.g., checking if the email is already in use)
        return true;
    }
}
