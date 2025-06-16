using AuthenticationProvider.Models.Requests;
using AuthenticationProvider.Models.Responses;

namespace AuthenticationProvider.Interfaces.Services;

/// <summary>
/// Handles the sign-up process for a user.
/// </summary>
public interface ISignUpService
{
    /// <summary>
    /// Registers a new user with the provided details.
    /// </summary>
    /// <param name="signInrequest">The user registration details.</param>
    /// <returns>A response containing the registration result.</returns>
    Task<SignUpResponse> RegisterUserAsync(SignUpRequest signInrequest);

    /// <summary>
    /// Deletes a user by its unique identifier.
    /// </summary>
    /// <param name="userId">The unique ID of the user to delete.</param>
    Task DeleteUserAsync(DeleteRequest deleteRequest);
}
