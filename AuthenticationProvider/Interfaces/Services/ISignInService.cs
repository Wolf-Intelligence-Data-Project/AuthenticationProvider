using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Interfaces.Utilities;

/// <summary>
/// Handles the sign-in process for a company.
/// </summary>
public interface ISignInService
{
    /// <summary>
    /// Authenticates a company based on the provided sign-in credentials.
    /// </summary>
    /// <param name="signInDto">The sign-in credentials.</param>
    /// <returns>A response containing authentication details.</returns>
    Task<SignInResponse> SignInAsync(SignInRequest signInDto);
}
