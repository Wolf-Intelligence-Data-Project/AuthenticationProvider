using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Services;

namespace AuthenticationProvider.Services;

/// <summary>
/// Service responsible for handling the sign-up process.
/// </summary>
public class SignUpService : ISignUpService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly IAddressRepository _addressRepository;
    private readonly ILogger<SignUpService> _logger;
    private readonly PasswordHasher<UserEntity> _passwordHasher;
    private readonly IEmailRestrictionService _emailRestrictionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ISignOutService _signOutService;

    public SignUpService(
        IUserRepository userRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationService accountVerificationService,
        IAddressRepository addressRepository,
        ILogger<SignUpService> logger,
        IEmailRestrictionService emailRestrictionService,
        IHttpContextAccessor httpContextAccessor,
        IAccessTokenService accessTokenService,
        ISignOutService signOutService,
        PasswordHasher<UserEntity> passwordHasher)
    {
        _userRepository = userRepository;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _addressRepository = addressRepository;
        _logger = logger;
        _passwordHasher = new PasswordHasher<UserEntity>();
        _emailRestrictionService = emailRestrictionService;
        _httpContextAccessor = httpContextAccessor;
        _accessTokenService = accessTokenService;
        _signOutService = signOutService;
    }

    public async Task<SignUpResponse> RegisterUserAsync(SignUpRequest request)
    {
        ValidateSignUpRequest(request);

        // Check if the email is restricted before proceeding
        if (_emailRestrictionService.IsRestrictedEmail(request.Email))
        {
            throw new InvalidOperationException("Den angivna e-posten är inte tillåten.");
        }

        if (await _userRepository.UserExistsAsync(request.IdentificationNumber, request.Email))
        {
            throw new InvalidOperationException("Ett företag med angivet organisationsnummer eller e-post finns redan.");
        }

        // Hash the password
        string hashedPassword = _passwordHasher.HashPassword(null, request.Password);

        var user = new UserEntity
        {
            IdentificationNumber = request.IdentificationNumber,
            IsCompany = request.IsCompany,
            CompanyName = request.IsCompany == true ? request.CompanyName : null!,
            Email = request.Email,
            BusinessType = request.IsCompany == true ? request.BusinessType : null!,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            TermsAndConditions = request.TermsAndConditions,
            IsVerified = false,
            PasswordHash = hashedPassword      
        };

        await _userRepository.AddAsync(user);

        // Add the primary and additional addresses
        await AddAddressesAsync(request, user);



        // Send the verification email
        var emailSent = await _accountVerificationService.SendVerificationEmailAsync(user.UserId.ToString());
        if (emailSent != ServiceResult.Success)
        {
            throw new InvalidOperationException("Det gick inte att skicka verifieringsmail.");
        }

        return new SignUpResponse
        {
            Success = true,
            UserId = user.UserId
        };
    }
    public async Task DeleteUserAsync(DeleteRequest deleteRequest)
    {
        var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AccessToken"];
        var userIdString = _accessTokenService.GetUserIdFromToken(token!);
        var password = deleteRequest.Password;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            throw new InvalidOperationException("Ogiltigt användar-ID.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Användaren hittades inte.");
        }
        // Verify the plain-text password using the injected PasswordHasher

        var passwordMatch = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (passwordMatch != PasswordVerificationResult.Success)
        {
            throw new UnauthorizedAccessException("Ogiltigt lösenord.");
        }

        var signOutResult = await _signOutService.SignOutAsync(userId.ToString());
        if (!signOutResult)
        {
            _logger.LogWarning("Failed to sign out the user");
        }

        await _userRepository.DeleteAsync(userId);
        _logger.LogInformation($"Användaren med ID {userId} har raderats.");
    }


    private async Task AddAddressesAsync(SignUpRequest request, UserEntity user)
    {
        // Retrieve all existing addresses associated with the user once
        var existingAddresses = await _addressRepository.GetAddressesByUserIdAsync(user.UserId);

        // Add primary address
        if (request.PrimaryAddress != null)
        {
            if (string.IsNullOrWhiteSpace(request.PrimaryAddress.StreetAndNumber) ||
                string.IsNullOrWhiteSpace(request.PrimaryAddress.City) ||
                string.IsNullOrWhiteSpace(request.PrimaryAddress.PostalCode))
            {
                throw new InvalidOperationException("Primary address is incomplete.");
            }

            // Check if the primary address already exists for the user
            if (existingAddresses.Any(a => a.StreetAndNumber == request.PrimaryAddress.StreetAndNumber &&
                                           a.City == request.PrimaryAddress.City &&
                                           a.PostalCode == request.PrimaryAddress.PostalCode))
            {
                throw new InvalidOperationException("Den här adressen finns redan för företaget.");
            }

            var primaryAddress = new AddressEntity
            {
                StreetAndNumber = request.PrimaryAddress.StreetAndNumber,
                City = request.PrimaryAddress.City,
                PostalCode = request.PrimaryAddress.PostalCode,
                UserId = user.UserId,
                Region = request.PrimaryAddress.Region,
                IsPrimary = true
            };

            await _addressRepository.AddAsync(primaryAddress);
        }

        // Add additional addresses
        if (request.AdditionalAddresses != null && request.IsCompany != null)
        {
            foreach (var additionalAddress in request.AdditionalAddresses)
            {
                if (string.IsNullOrWhiteSpace(additionalAddress.StreetAndNumber) ||
                    string.IsNullOrWhiteSpace(additionalAddress.City) ||
                    string.IsNullOrWhiteSpace(additionalAddress.PostalCode))
                {
                    throw new InvalidOperationException("Additional address is incomplete.");
                }

                // Check if the additional address already exists for the user
                if (existingAddresses.Any(a => a.StreetAndNumber == additionalAddress.StreetAndNumber &&
                                               a.City == additionalAddress.City &&
                                               a.PostalCode == additionalAddress.PostalCode))
                {
                    throw new InvalidOperationException("Den här adressen finns redan för företaget.");
                }

                var address = new AddressEntity
                {
                    StreetAndNumber = additionalAddress.StreetAndNumber,
                    City = additionalAddress.City,
                    PostalCode = additionalAddress.PostalCode,
                    UserId = user.UserId,
                    Region = additionalAddress.Region,
                    IsPrimary = false
                };

                await _addressRepository.AddAsync(address);
                _logger.LogInformation("Additional address added.");
            }
        }
    }

    private void ValidateSignUpRequest(SignUpRequest request)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        // Perform validation using data annotations
        bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // If the validation failed, throw an exception with the first error message
        if (!isValid)
        {
            var errorMessage = validationResults.FirstOrDefault()?.ErrorMessage;
            throw new InvalidOperationException(errorMessage ?? "Ogiltiga inloggningsuppgifter.");
        }
    }
}
