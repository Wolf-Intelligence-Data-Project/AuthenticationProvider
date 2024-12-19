using AuthenticationProvider.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Dtos;

public class SignUpDto
{
    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisationsnummer måste vara exakt 10 siffror.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Organisationsnummer får endast innehålla siffror.")]
    public string OrganisationNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Företagsnamnet får inte överstiga 100 tecken.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Ogiltigt e-postformat.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte överstiga 256 tecken.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(BusinessType), ErrorMessage = "Ogiltig företagstyp.")]
    public BusinessType BusinessType { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Ansvarig persons namn får inte överstiga 100 tecken.")]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [RegularExpression(@"^\+46\d{9}$", ErrorMessage = "Telefonnumret måste vara ett giltigt svenskt nummer som börjar med +46.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public bool TermsAndConditions { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$", ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Primary Address field (now using AddressDto)
    [Required(ErrorMessage = "Primär adress måste anges.")]
    public AddressDto PrimaryAddress { get; set; } = new AddressDto();

    // Optional additional addresses
    public List<AddressDto> AdditionalAddresses { get; set; } = new List<AddressDto>();
}