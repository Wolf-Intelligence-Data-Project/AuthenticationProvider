using Microsoft.AspNetCore.Identity;

namespace AuthenticationProvider.Data;

public class ApplicationUser : IdentityUser
{
    // Company properties
    public string CompanyName { get; set; } = string.Empty;
    public string OrganisationNumber { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;

    // Other properties
    public int? CompanyId { get; set; }
}