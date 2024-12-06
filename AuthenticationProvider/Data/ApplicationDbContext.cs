using AuthenticationProvider.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; } // Added DbSet for ResetPasswordToken

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Company entity
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Addresses)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict delete for addresses

        modelBuilder.Entity<Company>()
            .HasOne(c => c.PrimaryAddress)
            .WithOne()
            .HasForeignKey<Company>(c => c.PrimaryAddressId)
            .OnDelete(DeleteBehavior.SetNull); // SetNull delete for PrimaryAddressId

        // Configure ResetPasswordToken entity
        modelBuilder.Entity<ResetPasswordToken>(entity =>
        {
            // Explicit table name
            entity.ToTable("ResetPasswordTokens");

            // Primary key
            entity.HasKey(t => t.Id);

            // Relationships
            entity.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete tokens when a company is deleted

            // Required fields
            entity.Property(t => t.Token)
                .IsRequired();

            entity.Property(t => t.ExpiryDate)
                .IsRequired();

            // Default value for IsUsed
            entity.Property(t => t.IsUsed)
                .HasDefaultValue(false);
        });
    }
}
