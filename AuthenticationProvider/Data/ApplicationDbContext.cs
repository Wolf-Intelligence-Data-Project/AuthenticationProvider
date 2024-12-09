using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; }
    public DbSet<AccountVerificationToken> AccountVerificationTokens { get; set; } // Added DbSet for AccountVerificationToken

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Company entity
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

        // Configure ResetPasswordToken entity
        modelBuilder.Entity<ResetPasswordToken>(entity =>
        {
            entity.ToTable("ResetPasswordTokens");
            entity.HasKey(t => t.Id);

            entity.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ExpiryDate).IsRequired();
            entity.Property(t => t.IsUsed).HasDefaultValue(false);
        });

        // Configure AccountVerificationToken entity
        modelBuilder.Entity<AccountVerificationToken>(entity =>
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
