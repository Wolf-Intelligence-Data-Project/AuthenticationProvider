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

    public SignInService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISendVerificationService emailVerificationService,
        ILoginSessionTokenService loginSessionTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailVerificationService = emailVerificationService;
        _loginSessionTokenService = loginSessionTokenService;
    }

    public async Task<SignInResponse> SignUpAsync(SignUpRequest request)
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
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "User registration failed."
            };
        }

        // Generate a verification token for email confirmation
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Send the verification email
        var emailSent = await _emailVerificationService.SendVerificationEmailAsync(token);
        if (!emailSent)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Failed to send verification email."
            };
        }

        return new SignInResponse
        {
            Success = true,
            Message = "User registered successfully. Please check your email to verify your account."
        };
    }

    public async Task<SignInResponse> SignInAsync(SignInRequest request)
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
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        // Check if the email is confirmed (EmailConfirmed flag)
        if (!user.EmailConfirmed)
        {
            // Return response if the account is not verified
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Your account isn't verified yet. Please check your inbox or click below to resend the verification email.",
                Message = "Email not verified"
            };
        }

        // Attempt to sign in the user
        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
        if (!result.Succeeded)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
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

    // Helper method to validate the request model
    private List<string> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        bool isValid = Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        return validationResults.Select(result => result.ErrorMessage).ToList();
    }
}
