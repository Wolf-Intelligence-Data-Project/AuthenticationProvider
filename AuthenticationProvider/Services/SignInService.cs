using AuthenticationProvider.Models.Responses;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Models.Requests;

namespace AuthenticationProvider.Services;

/// <summary>
/// Service responsible for handling sign-in logic for user authentication.
/// </summary>
public class SignInService : ISignInService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;
    private readonly ILogger<SignInService> _logger;

    public SignInService(
        IUserRepository userRepository,
        IAccessTokenService accessTokenService,
        IPasswordHasher<UserEntity> passwordHasher,
        ILogger<SignInService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to sign in a user using email and password.
    /// Validates the provided credentials and generates an access token upon successful authentication.
    /// </summary>
    /// <param name="signInDto">The sign-in request containing email and password.</param>
    /// <returns>A response indicating success or failure, including an access token if successful.</returns>
    public async Task<SignInResponse> SignInAsync(SignInRequest signInRequest)
    {
        if (signInRequest == null)
        {
            _logger.LogWarning("SignInDto is null.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ogiltig inloggningsförfrågan."
            };
        }

        if (string.IsNullOrWhiteSpace(signInRequest.Email) || string.IsNullOrWhiteSpace(signInRequest.Password))
        {
            _logger.LogWarning("Sign-in failed: Email or password is empty.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "E-post och lösenord krävs."
            };
        }

        try
        {
            var userEntity = await _userRepository.GetByEmailAsync(signInRequest.Email);
            if (userEntity == null)
            {
                _logger.LogWarning("Sign-in failed: User not found for provided email.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Användaren finns inte."
                };
            }
         
            if (!ValidatePassword(userEntity, signInRequest.Password))
            {
                _logger.LogWarning("Sign-in failed: Invalid credentials for the provided email.");
                return new SignInResponse
                {
                    Success = false,
                    ErrorMessage = "Felaktiga inloggningsuppgifter."
                };
            }

            var token = _accessTokenService.GenerateAccessToken(userEntity);
            if (token == null)
            {
                return new SignInResponse
                {
                    Success = false,
                    Message = "Inloggning lyckades.",
                    User = userEntity, 

                };
            }

            _logger.LogInformation("User signed in successfully.");
            return new SignInResponse
            {
                Success = true,
                Message = "Inloggning lyckades.",
                User = userEntity,

            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during sign-in.");
            return new SignInResponse
            {
                Success = false,
                ErrorMessage = "Ett fel inträffade vid inloggning. Försök igen senare."
            };
        }
    }

    /// <summary>
    /// Validates the provided password against the stored hash.
    /// </summary>
    /// <param name="user">The user whose password needs to be verified.</param>
    /// <param name="providedPassword">The password provided by the user.</param>
    /// <returns>True if the password is valid; otherwise, false.</returns>
    private bool ValidatePassword(UserEntity user, string providedPassword)
    {
        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
        return passwordResult == PasswordVerificationResult.Success;
    }
}
