using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.SignIn;
using AuthenticationProvider.Models.SignUp;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthenticationProvider.Models;

namespace AuthenticationProvider.Services;

public class SignInService : ISignInService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccessTokenService _accessTokenService;  // AccessTokenService

    public SignInService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccountVerificationService accountVerificationService,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccessTokenService accessTokenService)  // Inject AccessTokenService
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountVerificationService = accountVerificationService;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accessTokenService = accessTokenService;  // Initialize AccessTokenService
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
        if (user == null || !user.EmailConfirmed)
        {
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password, or email not verified."
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

        // Generate the access token for authenticated user (using AccessTokenService)
        var accessToken = _accessTokenService.GenerateAccessToken(user);  // Generate token here

        return new SignInResponse
        {
            Success = true,
            Token = accessToken // Return token in response
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
