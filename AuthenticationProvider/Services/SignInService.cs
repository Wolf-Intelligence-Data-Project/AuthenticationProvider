using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.SignIn;
using AuthenticationProvider.Models.SignUp;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Services;

public class SignInService : ISignInService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ITokenService _tokenService;

    public SignInService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailVerificationService emailVerificationService,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailVerificationService = emailVerificationService;
        _tokenService = tokenService;
    }

    public async Task<SignInResponse> SignUpAsync(SignUpRequest request)
    {
        // Step 1: Validate the SignUpRequest model
        var validationResults = ValidateModel(request);
        if (validationResults.Any())
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", validationResults)
            };
        }

        // Step 2: Create a new user in the database
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

        // Step 3: Generate a verification token for email confirmation
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Step 4: Send the verification email
        var emailSent = await _emailVerificationService.SendVerificationEmailAsync(token);
        if (!emailSent)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Failed to send verification email."
            };
        }

        // Return successful response
        return new SignInResponse
        {
            Success = true,
            Message = "User registered successfully. Please check your email to verify your account."
        };
    }

    public async Task<SignInResponse> SignInAsync(SignInRequest request)
    {
        // Step 1: Validate the SignInRequest model
        var validationResults = ValidateModel(request);
        if (validationResults.Any())
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", validationResults)
            };
        }

        // Step 2: Find the user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.EmailConfirmed)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password, or email not verified."
            };
        }

        // Step 3: Attempt to sign in the user
        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
        if (!result.Succeeded)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        // Step 4: Generate the token for authenticated user
        var token = _tokenService.GenerateToken(user.Id, user.UserName);
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
