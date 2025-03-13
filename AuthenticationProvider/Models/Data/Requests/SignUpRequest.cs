using AuthenticationProvider.Services.Utilities;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class SignUpRequest
{

    [Required(ErrorMessage = "Organisationsnummer är obligatoriskt.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisationsnummer måste vara exakt 10 siffror.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Organisationsnummer får endast innehålla siffror.")]
    public string IdentificationNumber { get; set; } = null!;

    public bool? IsCompany { get; set; }  //Is this a user or person

    [RequiredBasedOnIsCompany]
    [StringLength(100, ErrorMessage = "Företagsnamn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*(?: [A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*)*)$", ErrorMessage = "Varje ord i företagsnamnet måste börja med en stor bokstav.")]
    public string CompanyName { get; set; } = null!;

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "E-postadressen är ogiltig.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte vara längre än 256 tecken.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "E-postadressen följer inte standardformatet.")]
    public string Email { get; set; } = null!;

    [RequiredBasedOnIsCompany]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäöA-ZÅÄÖ\s]*$", ErrorMessage = "Affärstyp får endast innehålla bokstäver och måste börja med en stor bokstav.")]
    [ValidateBusinessType(ErrorMessage = "Ogiltig affärstyp. Vänligen välj en giltig affärstyp.")]
    public string BusinessType { get; set; } = null!;

    [RequiredBasedOnIsCompany(ErrorMessage = "Ansvarig persons namn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Ansvarig persons namn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?\s)+([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?)$", ErrorMessage = "Varje ord i ansvarig persons namn måste börja med en stor bokstav. Bindestreck är tillåtet.")]
    [MinLength(2, ErrorMessage = "Ansvarig persons namn måste bestå av minst två ord.")]
    public string FullName { get; set; } = null!; //This is CEO name if IsCompany is true

    [Required(ErrorMessage = "Telefonnummer är obligatoriskt.")]
    [RegularExpression(@"^\+46\s7[02369](\s\d{2,3}){3}$", ErrorMessage = "Telefonnummer måste ha formatet +46 7X XXX XX XX eller liknande.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du måste acceptera villkoren.")]
    public bool TermsAndConditions { get; set; }

    [Required(ErrorMessage = "Lösenord krävs.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Lösenordet måste vara minst 8 tecken långt.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$",
        ErrorMessage = "Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Bekräfta lösenord krävs.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Primär adress måste anges.")]
    public AddressRequest PrimaryAddress { get; set; } = null!;

    public List<AddressRequest>? AdditionalAddresses { get; set; }

}
