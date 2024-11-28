using AuthenticationProvider.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Address> Addresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>()
            .HasMany(c => c.Addresses)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Company>()
            .HasOne(c => c.PrimaryAddress)
            .WithOne()
            .HasForeignKey<Company>(c => c.PrimaryAddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
