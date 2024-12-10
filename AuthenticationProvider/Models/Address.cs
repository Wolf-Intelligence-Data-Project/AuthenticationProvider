using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models
{
    public class Address
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
        public Region Region { get; set; }

        [Required]
        public Guid CompanyId { get; set; }

        public Company? Company { get; set; }

        [Required]
        public string AddressType { get; set; }

        // Add validation logic to make it optional for non-primary addresses
        public bool IsPrimary { get; set; }
    }
}