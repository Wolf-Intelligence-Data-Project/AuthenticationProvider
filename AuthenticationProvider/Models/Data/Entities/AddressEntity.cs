using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Entities;

public class AddressEntity
{
    [Key]
    public Guid AddressId { get; set; }

    [Required(ErrorMessage = "Gatuadress är obligatorisk.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäö]*(?:\s[A-ZÅÄÖa-zåäö0-9]*)*)$", ErrorMessage = "Varje ord i gatuadressen måste börja med en stor bokstav och kan innehålla siffror.")]
    public string StreetAndNumber { get; set; }

    [Required(ErrorMessage = "Postnummer är obligatoriskt.")]
    [RegularExpression(@"^\d{3} \d{2}$", ErrorMessage = "Postnummer måste vara exakt 5 siffror och innehålla ett mellanslag efter tredje siffran.")]
    public string PostalCode { get; set; }

    [Required(ErrorMessage = "Stad är obligatorisk.")]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäö]+(?:\s[A-ZÅÄÖ][a-zåäö]+)*$", ErrorMessage = "Stad måste börja med en stor bokstav.")]
    public string City { get; set; }

    [Required(ErrorMessage = "Region är obligatorisk.")]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäö]+(?:\s[A-ZÅÄÖ][a-zåäö]+)*$", ErrorMessage = "Varje ord i regionen måste börja med en stor bokstav och kan innehålla mellanslag.")]
    public string Region { get; set; }

    [Required(ErrorMessage = "Företags-ID är obligatoriskt.")]
    public Guid UserId { get; set; }
    public UserEntity? User { get; set; }
    public bool IsPrimary { get; set; }
}
