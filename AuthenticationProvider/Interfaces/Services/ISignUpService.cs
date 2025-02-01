using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;

namespace AuthenticationProvider.Interfaces.Utilities;

/// <summary>
/// Handles the sign-up process for a company.
/// </summary>
public interface ISignUpService
{
    /// <summary>
    /// Registers a new company with the provided details.
    /// </summary>
    /// <param name="request">The company registration details.</param>
    /// <returns>A response containing the registration result.</returns>
    Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request);

    /// <summary>
    /// Deletes a company by its unique identifier.
    /// </summary>
    /// <param name="companyId">The unique ID of the company to delete.</param>
    Task DeleteCompanyAsync(Guid companyId);
}
