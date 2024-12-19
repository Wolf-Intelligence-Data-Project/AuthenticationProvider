using AuthenticationProvider.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Dtos;

public class AddressDto
{
    [Required(ErrorMessage = "Gatuadress är obligatorisk.")]
    public string StreetAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postnummer är obligatoriskt.")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Postnumret måste bestå av 5 siffror.")]
    [RegularExpression(@"^\d{3}\s?\d{2}$", ErrorMessage = "Postnummer måste bestå av 5 siffror, med ett valfritt mellanslag (t.ex. 123 45).")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stad är obligatorisk.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region är obligatorisk.")]
    [EnumDataType(typeof(Region), ErrorMessage = "Välj en giltig region.")]
    public Region Region { get; set; } // Region is required in AddressDto
}