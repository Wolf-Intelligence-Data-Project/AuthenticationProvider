using System.ComponentModel.DataAnnotations;

public class AddressRequest
{
    [Required]
    public string StreetAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Postal code must be 5 digits.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    public string? Region { get; set; }
}
