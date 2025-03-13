using AuthenticationProvider.Models.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationProvider.Models.Data;


// Needed because of Identity
public class ApplicationUser : IdentityUser
{
    // User properties
    public string CompanyName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;

    //// Other properties
    //public int? UserId { get; set; }
};
