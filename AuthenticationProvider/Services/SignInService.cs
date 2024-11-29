using AuthenticationProvider.Models.SignIn;
using AuthenticationProvider.Models.SignUp;
using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using AuthenticationProvider.Interfaces.Services;

namespace AuthenticationProvider.Services;

public class SignInService : ISignInService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISendVerificationService _emailVerificationService;
    private readonly ILoginSessionTokenService _loginSessionTokenService;
    private readonly ILogger<SignInService> _logger;

    public SignInService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISendVerificationService emailVerificationService,
        ILoginSessionTokenService loginSessionTokenService,
        ILogger<SignInService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailVerificationService = emailVerificationService;
        _loginSessionTokenService = loginSessionTokenService;
        _logger = logger;
    }

    public async Task<SignInResponse> SignUpAsync(SignUpRequest request)
    {
        try
        {
            // Validate the SignUpRequest model
            var validationResults = ValidateModel(request);
            if (validationResults.Any())
            {
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResults)
                };
            }

            // Create a new user in the database
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogError("User registration failed.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Registreringen misslyckades."
                };
            }

            // Generate a verification token for email confirmation
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Send the verification email
            var emailSent = await _emailVerificationService.SendVerificationEmailAsync(token);
            if (!emailSent)
            {
                _logger.LogError("Failed to send verification email.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Kunde inte skicka verifierings-e-post."
                };
            }

            return new SignInResponse
            {
                Success = true,
                Message = "Användare registrerad. Vänligen kontrollera din e-post för att verifiera ditt konto."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during SignUp: {ex.Message}");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ett fel inträffade vid registreringen."
            };
        }
    }

    public async Task<SignInResponse> SignInAsync(SignInRequest request)
    {
        try
        {
            // Validate the SignInRequest model
            var validationResults = ValidateModel(request);
            if (validationResults.Any())
            {
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResults)
                };
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning($"Invalid login attempt with email: {request.Email}");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Ogiltig e-postadress eller lösenord."
                };
            }

            // Check if the email is confirmed (EmailConfirmed flag)
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Unverified login attempt for email: {request.Email}");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Ditt konto är inte verifierat. Vänligen kontrollera din e-post eller klicka nedan för att skicka verifierings-e-post igen.",
                    Message = "E-posten är inte verifierad."
                };
            }

            // Attempt to sign in the user
            var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed login attempt.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Ogiltig e-postadress eller lösenord."
                };
            }

            // Generate the login session token for authenticated user
            var token = await _loginSessionTokenService.GenerateLoginSessionTokenAsync(request.Email);
            return new SignInResponse
            {
                Success = true,
                Token = token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during SignIn: {ex.Message}");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ett fel inträffade vid inloggningen."
            };
        }
    }

    // Helper method to validate the request model
    private List<string> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        bool isValid = Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        return validationResults.Select(result => result.ErrorMessage).ToList();
    }
}
