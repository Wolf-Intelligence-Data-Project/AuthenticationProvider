using System.Collections.Generic;

namespace AuthenticationProvider.Models;

public class Company
{
    public int Id { get; set; }

    // Organisation Number (Autopopulated from external API)
    public string OrganisationNumber { get; set; } = null!;

    // Company Name
    public string CompanyName { get; set; } = null!;

    // Email address of the company
    public string Email { get; set; } = null!;

    // Business type/category for future product connections
    public string BusinessType { get; set; } = null!;

    // Name of the responsible person (could be CEO, owner, etc.)
    public string ResponsiblePersonName { get; set; } = null!;

    // Contact phone number for the company
    public string PhoneNumber { get; set; } = null!;

    // Whether the company is verified via email (for access control)
    public bool IsVerified { get; set; } = false;

    // One-to-many relationship with Address
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    // Reference to the primary address
    public Address PrimaryAddress { get; set; } = null!;

    // Consent checkbox for personal data processing
    public bool TermsAndConditions { get; set; } = false;
}
