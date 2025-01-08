using AuthenticationProvider.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Data.Entities;

public class CompanyEntity
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
    public string BusinessType { get; set; }

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
    public AddressEntity? PrimaryAddress { get; set; } // Optional

    // All other addresses associated with the company
    public ICollection<AddressEntity> Addresses { get; set; } = new List<AddressEntity>();

    [Required]
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
}
