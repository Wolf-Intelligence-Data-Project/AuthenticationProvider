using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationProvider.Models.Data.Entities;

public class ResetPasswordTokenEntity
{
    
        [Key]
        [Required]
        public Guid Id { get; set; }  // Primary Key for the token entity

        [Required]
        public Guid UserId { get; set; } // Explicit foreign key to UserEntity

        [Required]
        public string Token { get; set; } = null!;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public bool IsUsed { get; set; } = false;

        [ForeignKey("UserId")] // Foreign key linking to UserEntity
        [Required]
        public UserEntity User { get; set; } = null!;
}