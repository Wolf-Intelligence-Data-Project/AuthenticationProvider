using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Models;

public class Company
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(10, MinimumLength = 10)]
    public string OrganisationNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public BusinessType BusinessType { get; set; } = BusinessType.Unspecified;

    [Required]
    [StringLength(100)]
    public string ResponsiblePersonName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = false;

    [Required]
    public bool TermsAndConditions { get; set; }

    // Linking Primary Address
    public int? PrimaryAddressId { get; set; } // Optional

    [ForeignKey("PrimaryAddressId")]
    public Address? PrimaryAddress { get; set; } // Optional

    // All other addresses associated with the company
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    [Required]
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
}
