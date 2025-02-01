using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests;

public class AddressRequest
{
    [Required(ErrorMessage = "Gatuadress är obligatorisk.")]
    [RegularExpression(@"^([A-ZÅÄÖ][a-zåäö]*(?:\s[A-ZÅÄÖa-zåäö0-9]*)*)$", ErrorMessage = "Varje ord i gatuadressen måste börja med en stor bokstav och kan innehålla siffror.")]
    public string StreetAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postnummer är obligatoriskt.")]
    [RegularExpression(@"^\d{3} \d{2}$", ErrorMessage = "Postnummer måste vara exakt 5 siffror och innehålla ett mellanslag efter tredje siffran.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stad är obligatorisk.")]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäö]+(?:\s[A-ZÅÄÖ][a-zåäö]+)*$", ErrorMessage = "Stad måste börja med en stor bokstav.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region är obligatorisk.")]
    [RegularExpression(@"^[A-ZÅÄÖ][a-zåäö]+(?:\s[A-ZÅÄÖ][a-zåäö]+)*$", ErrorMessage = "Varje ord i regionen måste börja med en stor bokstav och kan innehålla mellanslag.")]
    public string Region { get; set; } = string.Empty;
}
