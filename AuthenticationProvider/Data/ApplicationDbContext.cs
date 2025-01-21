using AuthenticationProvider.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets for your entities
    public DbSet<CompanyEntity> Companies { get; set; }
    public DbSet<AddressEntity> Addresses { get; set; }
    public DbSet<ResetPasswordTokenEntity> ResetPasswordTokens { get; set; }
    public DbSet<AccountVerificationTokenEntity> AccountVerificationTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the CompanyEntity relationships
        modelBuilder.Entity<CompanyEntity>()
            .HasMany(c => c.Addresses)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade); // Delete addresses when the company is deleted

        // No need for a relationship between CompanyEntity and PrimaryAddressId anymore

        // Configure the AddressEntity
        modelBuilder.Entity<AddressEntity>(entity =>
        {
            entity.HasKey(a => a.Id); // Primary key
            entity.Property(a => a.StreetAddress).IsRequired();
            entity.Property(a => a.PostalCode).IsRequired();
            entity.Property(a => a.City).IsRequired();
            entity.Property(a => a.Region).IsRequired();

            // Add the IsPrimary flag to mark the primary address
            entity.Property(a => a.IsPrimary).HasDefaultValue(false); // Default value to false for non-primary addresses
        });

        // Configure the ResetPasswordTokenEntity
        modelBuilder.Entity<ResetPasswordTokenEntity>(entity =>
        {
            entity.ToTable("ResetPasswordTokens"); // Explicit table name
            entity.HasKey(t => t.Id); // Primary key

            entity.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // Delete tokens when a company is deleted

            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ExpiryDate).IsRequired();
            entity.Property(t => t.IsUsed).HasDefaultValue(false);
        });

        // Configure the AccountVerificationTokenEntity
        modelBuilder.Entity<AccountVerificationTokenEntity>(entity =>
        {
            entity.ToTable("AccountVerificationTokens"); // Explicit table name
            entity.HasKey(t => t.Id); // Primary key

            entity.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete tokens when a company is deleted

            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ExpiryDate).IsRequired();
            entity.Property(t => t.IsUsed).HasDefaultValue(false);
        });
    }

}
