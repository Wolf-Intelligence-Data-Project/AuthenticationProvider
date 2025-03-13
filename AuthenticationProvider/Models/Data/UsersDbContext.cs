using AuthenticationProvider.Models.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Models.Data;

public class UserDbContext : IdentityDbContext<ApplicationUser>
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    // DbSets for your entities
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<AddressEntity> Addresses { get; set; }
    public DbSet<ResetPasswordTokenEntity> ResetPasswordTokens { get; set; }
    public DbSet<AccountVerificationTokenEntity> AccountVerificationTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the UserEntity relationships
        modelBuilder.Entity<UserEntity>()
            .HasMany(c => c.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Delete addresses when the user is deleted

        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.IdentificationNumber)
            .IsUnique();

        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // No need for a relationship between UserEntity and PrimaryAddressId anymore

        // Configure the AddressEntity
        modelBuilder.Entity<AddressEntity>(entity =>
        {
            entity.HasKey(a => a.AddressId); // Primary key
            entity.Property(a => a.StreetAndNumber).IsRequired();
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

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete tokens when a user is deleted

            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ExpiryDate).IsRequired();
            entity.Property(t => t.IsUsed).HasDefaultValue(false);
        });

        // Configure the AccountVerificationTokenEntity
        modelBuilder.Entity<AccountVerificationTokenEntity>(entity =>
        {
            entity.ToTable("AccountVerificationTokens"); // Explicit table name
            entity.HasKey(t => t.Id); // Primary key

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete tokens when a user is deleted

            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ExpiryDate).IsRequired();
            entity.Property(t => t.IsUsed).HasDefaultValue(false);
        });
    }

}
