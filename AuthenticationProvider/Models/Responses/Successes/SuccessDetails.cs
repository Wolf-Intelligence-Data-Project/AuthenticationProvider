namespace AuthenticationProvider.Models.Responses.Successes;

/// <summary>
/// This class contains additional details for each success message, 
/// providing more context to the user about the action's outcome.
/// </summary>
public static class SuccessDetails
{
    /// <summary>
    /// A general success detail message indicating that an action was completed successfully.
    /// </summary>
    public static readonly string ActionSuccessfulDetails = "Åtgärden har genomförts utan problem.";

    /// <summary>
    /// Success details for the registration process, indicating that user data was saved correctly.
    /// </summary>
    public static readonly string RegistrationSuccessfulDetails = "Dina uppgifter har registrerats framgångsrikt i vårt system.";

    /// <summary>
    /// Details confirming that a user's email address has been updated.
    /// </summary>
    public static readonly string EmailUpdatedDetails = "Den nya e-postadressen är nu aktiv.";

    /// <summary>
    /// Details confirming that a password has been successfully changed.
    /// </summary>
    public static readonly string PasswordChangedDetails = "Du kan nu logga in med det nya lösenordet.";

    /// <summary>
    /// Details confirming that the account has been verified successfully.
    /// </summary>
    public static readonly string AccountVerifiedDetails = "Kontot har verifierats och är nu aktiverat.";

    /// <summary>
    /// Details indicating that a request has been processed and completed without issues.
    /// </summary>
    public static readonly string RequestProcessedDetails = "Din förfrågan har bearbetats och avslutats framgångsrikt.";

    /// <summary>
    /// Details confirming that the company has been registered successfully.
    /// </summary>
    public static readonly string CompanyRegisteredDetails = "Företaget har blivit korrekt registrerat och är nu klart för användning.";

    /// <summary>
    /// Details confirming that the password reset has been completed.
    /// </summary>
    public static readonly string PasswordResetSuccessfulDetails = "Det nya lösenordet har registrerats och är klart att användas.";

    /// <summary>
    /// Details confirming that the email change has been successfully processed.
    /// </summary>
    public static readonly string EmailChangeSuccessfulDetails = "Den nya e-postadressen har aktiverats och kommer att användas för framtida meddelanden.";

    /// <summary>
    /// Details confirming that the verification email has been sent to the user.
    /// </summary>
    public static readonly string VerificationEmailSentDetails = "En verifieringslänk har skickats till användarens e-postadress.";
}