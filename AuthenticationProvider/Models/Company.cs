public class Company
{
    public int Id { get; set; }
    public string OrganisationNumber { get; set; }
    public string CompanyName { get; set; }
    public string Email { get; set; }
    public string BusinessType { get; set; }
    public string ResponsiblePersonName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsVerified { get; set; }
    public bool TermsAndConditions { get; set; }

    // Nullable PrimaryAddressId
    public int? PrimaryAddressId { get; set; }
    public Address PrimaryAddress { get; set; }

    // Navigation property
    public ICollection<Address> Addresses { get; set; }
}
