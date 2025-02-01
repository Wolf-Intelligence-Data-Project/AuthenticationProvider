using AuthenticationProvider.Services.Utilities;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Entities;

public class CompanyEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Organisationsnummer är obligatoriskt.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisationsnummer måste vara exakt 10 siffror.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Organisationsnummer får endast innehålla siffror.")]
    public string OrganizationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Företagsnamn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Företagsnamn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*(?: [A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*)*)$", ErrorMessage = "Varje ord i företagsnamnet måste börja med en stor bokstav.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "E-postadressen är ogiltig.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte vara längre än 256 tecken.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "E-postadressen följer inte standardformatet.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Affärstyp är obligatorisk.")]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäöA-ZÅÄÖ\s]*$", ErrorMessage = "Affärstyp får endast innehålla bokstäver och måste börja med en stor bokstav.")]
    [ValidateBusinessType(ErrorMessage = "Ogiltig affärstyp. Vänligen välj en giltig affärstyp.")]
    public string BusinessType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ansvarig persons namn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Ansvarig persons namn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?\s)+([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?)$", ErrorMessage = "Varje ord i ansvarig persons namn måste börja med en stor bokstav. Bindestreck är tillåtet.")]
    [MinLength(2, ErrorMessage = "Ansvarig persons namn måste bestå av minst två ord.")]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefonnummer är obligatoriskt.")]
    [RegularExpression(@"^\+46\s7[02369](\s\d{2,3}){3}$", ErrorMessage = "Telefonnummer måste ha formatet +46 7X XXX XX XX eller liknande.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;

    [Required(ErrorMessage = "Du måste acceptera villkoren.")]
    public bool TermsAndConditions { get; set; }

    public ICollection<AddressEntity> Addresses { get; set; } = new List<AddressEntity>();

    [Required(ErrorMessage = "Lösenord är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Lösenord får inte vara längre än 100 tecken.")]
    public string PasswordHash { get; set; } = string.Empty;
}
