namespace AuthenticationProvider.Models.Responses.Errors;

/// <summary>
/// Represents the structure of an error response that is returned when an error occurs in the system.
/// This class includes an error code, user-friendly message, detailed error explanation.
/// </summary>
public class ErrorDefinition
{
    /// <summary>
    /// The unique error code associated with the error, used for identifying the type of error.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// A user-friendly message describing the error that can be displayed to the user.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// A detailed explanation of the error, useful for debugging or understanding the root cause.
    /// </summary>
    public string ErrorDetails { get; }

    /// <summary>
    /// Constructor to create an error definition.
    /// </summary>
    /// <param name="errorCode">The error code associated with the error.</param>
    /// <param name="errorMessage">The user-friendly error message.</param>
    /// <param name="errorDetails">The detailed explanation for developers.</param>
    public ErrorDefinition(string errorCode, string errorMessage, string errorDetails)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }
}
