using System.ComponentModel.DataAnnotations;
using AuthenticationProvider.Attributes;

namespace AuthenticationProvider.Models;

public class Address
{
    [Key]
    public int Id { get; set; }

    // Make StreetAddress optional, as Address is optional
    public string? StreetAddress { get; set; } = string.Empty;

    // Make PostalCode optional
    public string? PostalCode { get; set; } = string.Empty;

    // Make City optional
    public string? City { get; set; } = string.Empty;

    // Make Region nullable so it's optional if Address is not provided
    public Region? Region { get; set; }

    // CompanyId is required if Address exists, so leave it as required
    public Guid? CompanyId { get; set; }  // Make nullable if Address is optional

    // Navigation property to Company (not required for Address to be valid)
    public Company? Company { get; set; }

    // AddressType is optional, so no need to make it required
    public string? AddressType { get; set; }
}