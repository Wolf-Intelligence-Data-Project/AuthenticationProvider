using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Models.Responses;

public class SignInResponse
{
    public bool Success { get; set; }

    public string Message { get; set; }
    public UserEntity User { get; set; }
    public string ErrorMessage { get; set; }
}
