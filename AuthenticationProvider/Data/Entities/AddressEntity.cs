using AuthenticationProvider.Models;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Data.Entities
{
    public class AddressEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        [Required]
        public Guid CompanyId { get; set; }

        public CompanyEntity? Company { get; set; }

        [Required]
        public string AddressType { get; set; }

        // Add validation logic to make it optional for non-primary addresses
        public bool IsPrimary { get; set; }
    }
}