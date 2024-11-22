using Microsoft.EntityFrameworkCore;
using AuthenticationProvider.Models;

namespace AuthenticationProvider.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Address> Addresses { get; set; }
}
