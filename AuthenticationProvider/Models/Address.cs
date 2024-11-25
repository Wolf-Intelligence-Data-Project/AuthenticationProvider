using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class Address
{
    public int Id { get; set; }
    [Required]
    public string StreetAddress { get; set; }
    [Required]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Postal code must be 5 digits.")]
    public string PostalCode { get; set; }
    [Required]
    public string City { get; set; }
    public string? Region { get; set; }
    [Required]
    public string Country { get; set; }
    public int? CompanyId { get; set; }
    public Company Company { get; set; }
    public string? AddressType { get; set; }
}