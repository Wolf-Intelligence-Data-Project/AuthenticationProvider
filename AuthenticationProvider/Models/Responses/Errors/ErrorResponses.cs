namespace AuthenticationProvider.Models.Responses.Errors
{
    public static class ErrorResponses
    {
        public static readonly ErrorDefinition GeneralError = new(
            "UNKNOWN_ERROR",
            "Ett fel inträffade. Försök igen senare.",
            "An unexpected error occurred. Please try again later. If the issue persists, contact support."
        );

        public static readonly ErrorDefinition ModelStateError = new(
            "MODELSTATE_ERROR",
            "Ogiltig indata.",
            "The information you provided is not valid. Please check and try again."
        );

        public static readonly ErrorDefinition TokenGenerationFailed = new(
            "TOKEN_GENERATION_FAILED",
            "Fel vid tokengenerering.",
            "An error occurred while generating the token. Please try again later."
        );


        public static readonly ErrorDefinition InvalidCredentials = new(
            "INVALID_CREDENTIALS",
            "Ogiltiga inloggningsuppgifter.",
            "The login details you provided are incorrect. Please check your credentials and try again."
        );

        public static readonly ErrorDefinition TokenExpiredOrInvalid = new(
            "TOKEN_EXPIRED_OR_INVALID",
            "Din session har gått ut eller är ogiltig.",
            "Your session has expired or is no longer valid. Please log in again."
        );

        public static readonly ErrorDefinition CompanyNotFound = new(
            "COMPANY_NOT_FOUND",
            "Företaget kunde inte hittas.",
            "We couldn't find the company. Please ensure the company information is correct."
        );

        public static readonly ErrorDefinition EmailAlreadyInUse = new(
            "EMAIL_ALREADY_IN_USE",
            "E-postadressen är redan registrerad.",
            "The email address is already registered. Please use a different email address."
        );

        public static readonly ErrorDefinition EmailSendFailure = new(
            "EMAIL_SENDING_FAILED",
            "Det gick inte att skicka e-post.",
            "There was an issue sending the email. Please try again later."
        );

        public static readonly ErrorDefinition EmailNotFound = new(
            "EMAIL_NOT_FOUND",
            "E-postadress hittades inte.",
            "The email address was not found. Please ensure the email is correct."
        );

        public static readonly ErrorDefinition VerificationFailed = new(
            "VERIFICATION_FAILED",
            "Verifieringen misslyckades.",
            "Something went wrong during the verification process. Please try again later."
        );

        public static readonly ErrorDefinition ActionFailed = new(
            "ACTION_FAILED",
            "Åtgärden misslyckades. Försök igen.",
            "The action failed. Please try again. If the issue persists, contact support."
        );

        public static readonly ErrorDefinition UnauthorizedAccess = new(
            "UNAUTHORIZED_ACCESS",
            "Du har inte rätt behörighet att utföra denna åtgärd.",
            "You don't have permission to perform this action. Please contact your administrator."
        );

        public static readonly ErrorDefinition SessionIssue = new(
            "SESSION_ISSUE",
            "Det är ett problem med din session.",
            "There seems to be an issue with your session. Please log in again."
        );

        public static readonly ErrorDefinition TokenInvalidationError = new(
            "TOKEN_INVALIDATION_ERROR",
            "Ett fel inträffade vid hantering av din begäran.",
            "An error occurred while processing your request. Please try again later."
        );

        public static readonly ErrorDefinition InvalidInput = new(
            "INVALID_INPUT",
            "De uppgifter du angav är ogiltiga. Kontrollera och försök igen.",
            "The information you entered is invalid. Please check all fields and try again."
        );

        public static readonly ErrorDefinition NotFound = new(
            "NOT_FOUND",
            "Vi kunde inte hitta den begärda informationen.",
            "The requested information could not be found. Please ensure your request is correct."
        );

        public static readonly ErrorDefinition PasswordError = new(
            "PASSWORD_ERROR",
            "Det gick inte att ändra lösenordet. Försök igen.",
            "There was an issue updating your password. Please ensure your new password is correct and try again."
        );

        public static readonly ErrorDefinition SessionExpired = new(
            "SESSION_EXPIRED",
            "Din session har löpt ut. Logga in igen för att fortsätta.",
            "Your session has expired. Please log in again to continue."
        );

        public static readonly ErrorDefinition InternalServerError = new(
            "INTERNAL_SERVER_ERROR",
            "Ett internt serverfel inträffade. Försök igen senare.",
            "An internal server error occurred. Please try again later. If the issue persists, contact support."
        );

        public static readonly ErrorDefinition AccessDenied = new(
            "ACCESS_DENIED",
            "Åtkomst nekad. Du har inte behörighet att se denna information.",
            "Access to the requested resource is denied. Please ensure you have the necessary permissions."
        );

        public static readonly ErrorDefinition EmailExists = new(
            "EMAIL_EXISTS",
            "E-postadressen finns redan.",
            "The email address is already registered. Please ensure the email is unique and not already in use."
        );

        public static readonly ErrorDefinition EmailUpdateFailed = new(
            "EMAIL_UPDATE_FAILED",
            "E-postadress kan inte uppdateras.",
            "We couldn't update the email address. Please try again later."
        );

        public static readonly ErrorDefinition InvalidEmailFormat = new(
            "INVALID_EMAIL_FORMAT",
            "Ogiltig e-postadress.",
            "The email address format is incorrect. Please check and try again."
        );

        public static readonly ErrorDefinition MissingParameter = new(
            "MISSING_PARAMETER",
            "Nödvändig parameter saknas.",
            "A required parameter is missing. Please ensure all fields are filled and try again."
        );

        public static ErrorDefinition GeneralInternalError(Exception ex)
        {
            return new ErrorDefinition(
                "INVALID_OPERATION",
                "Ett internt fel inträffade under processen.",
                $"An internal error occurred during the process. Details: {ex.Message} Stack Trace: {ex.StackTrace}"
            );
        }

        public static readonly ErrorDefinition DatabaseConnectionError = new(
            "DATABASE_CONNECTION_ERROR",
            "Vi har problem i vårt system just nu.",
            "We are unable to connect to our servers. Please try again later."
        );

        public static readonly ErrorDefinition TimeoutError = new(
            "TIMEOUT_ERROR",
            "Tidsgränsen för begäran har överskridits.",
            "The request timed out. Please try again later."
        );

        public static readonly ErrorDefinition TestEnvironmentError = new(
            "TEST_ENVIRONMENT_ERROR",
            "Testmiljön har ett problem.",
            "There's an issue in the test environment. Please contact support for assistance."
        );
    }
}
