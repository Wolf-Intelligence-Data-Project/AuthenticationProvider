using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public class Address
{
    public int Id { get; set; }

    [Required]
    public string StreetAddress { get; set; }

    [Required]
    public string PostalCode { get; set; }

    [Required]
    public string City { get; set; }

    [Required]
    public string Region { get; set; }

    [Required]
    public int CompanyId { get; set; }

    public Company Company { get; set; }

    public string? AddressType { get; set; }
}