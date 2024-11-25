using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
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
        // Step 1: Create a new user in the database
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

        // Step 2: Generate a verification token for email confirmation
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Step 3: Send the verification email
        var emailSent = await _emailVerificationService.SendVerificationEmailAsync(request.Email, token);
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

        // Generate the token for authenticated user
        var token = _tokenService.GenerateToken(user.Id, user.UserName);
        return new SignInResponse
        {
            Success = true,
            Token = token
        };
    }
}
