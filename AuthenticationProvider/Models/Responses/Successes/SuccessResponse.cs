namespace AuthenticationProvider.Models.Responses.Successes;

/// <summary>
/// Represents the structure of a success response returned when an operation completes successfully.
/// This class includes a user-friendly message and optional additional details to provide complete feedback on the success.
/// </summary>
public class SuccessResponse
{
    /// <summary>
    /// A user-friendly message describing the success of the operation.
    /// This message can be displayed to the user to confirm that the action was completed successfully.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Additional details or explanation about the success, providing more context if necessary.
    /// This could be helpful for debugging or understanding the results in more detail.
    /// </summary>
    public string Detail { get; set; }
}
