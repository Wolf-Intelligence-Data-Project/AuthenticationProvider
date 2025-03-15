using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models.Data.Requests
{
    public class SendResetPasswordRequest
    {
        [Required]
        public string ResetId { get; set; }
    }
}
