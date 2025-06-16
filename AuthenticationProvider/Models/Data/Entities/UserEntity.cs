using AuthenticationProvider.Services.Utilities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Entities;

public class UserEntity : IdentityUser<Guid>
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid()!;

    [Required(ErrorMessage = "Organisationsnummer är obligatoriskt.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Organisationsnummer måste vara exakt 10 siffror.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Organisationsnummer får endast innehålla siffror.")]
    public string IdentificationNumber { get; set; }

    [Required(ErrorMessage = "Vänligen ange om du registrerar dig som privatperson eller företag.")]
    public bool IsCompany { get; set; } // Is the user a company or a person

    [RequiredBasedOnIsCompany]
    [StringLength(100, ErrorMessage = "Företagsnamn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*(?: [A-ZÅÄÖ][a-zåäöA-ZÅÄÖ0-9]*)*)$", ErrorMessage = "Varje ord i företagsnamnet måste börja med en stor bokstav.")]
    public string CompanyName { get; set; } = "";

    [Required(ErrorMessage = "E-postadress är obligatorisk.")]
    [EmailAddress(ErrorMessage = "E-postadressen är ogiltig.")]
    [StringLength(256, ErrorMessage = "E-postadressen får inte vara längre än 256 tecken.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "E-postadressen följer inte standardformatet.")]
    public string Email { get; set; }

    [RequiredBasedOnIsCompany]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäöA-ZÅÄÖ\s]*$", ErrorMessage = "Affärstyp får endast innehålla bokstäver och måste börja med en stor bokstav.")]
    [ValidateBusinessType(ErrorMessage = "Ogiltig affärstyp. Vänligen välj en giltig affärstyp.")]
    public string BusinessType { get; set; } = "";

    [Required(ErrorMessage = "Fullständigt namn är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Ansvarig persons namn får inte vara längre än 100 tecken.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?\s)+([A-ZÅÄÖ][a-zåäö]*(?:-[A-ZÅÄÖ][a-zåäö]*)?)$", ErrorMessage = "Varje ord i ansvarig persons namn måste börja med en stor bokstav. Bindestreck är tillåtet.")]
    [MinLength(2, ErrorMessage = "Ansvarig persons namn måste bestå av minst två ord.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Telefonnummer är obligatoriskt.")]
    [RegularExpression(@"^\+46\s7[02369](\s\d{2,3}){3}$", ErrorMessage = "Telefonnummer måste ha formatet +46 7X XXX XX XX eller liknande.")]
    public string PhoneNumber { get; set; }

    public bool IsVerified { get; set; } = false;

    [Required(ErrorMessage = "Du måste acceptera villkoren.")]
    public bool TermsAndConditions { get; set; } = false!;

    [Required(ErrorMessage = "Lösenord är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Lösenord får inte vara längre än 100 tecken.")]
    public string PasswordHash { get; set; }

    public DateTime RegisteredAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));

    public string? AdminNote { get; set; }  // Note about certain user for admin use

    public ICollection<AddressEntity> Addresses { get; set; } = new List<AddressEntity>();
    public ICollection<ResetPasswordTokenEntity> ResetPasswordTokens { get; set; } = new List<ResetPasswordTokenEntity>();
    public ICollection<EmailVerificationTokenEntity> EmailVerificationTokens { get; set; } = new List<EmailVerificationTokenEntity>();
}
