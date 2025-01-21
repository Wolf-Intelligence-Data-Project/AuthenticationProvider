using AuthenticationProvider.Data;

namespace AuthenticationProvider.Models.Responses;

public class SignInResponse
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string Message { get; set; }
    public ApplicationUser User { get; set; }
    public string ErrorMessage { get; set; }
}
