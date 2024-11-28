using AuthenticationProvider.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.SignUp;
using AuthenticationProvider.Models.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

public class SignUpService : ISignUpService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly ISendVerificationService _emailVerificationService;
    private readonly ILogger<SignUpService> _logger;
    private readonly PasswordHasher<Company> _passwordHasher;

    public SignUpService(
        ICompanyRepository companyRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        ISendVerificationService emailVerificationService,
        ILogger<SignUpService> logger)
    {
        _companyRepository = companyRepository;
        _accountVerificationTokenService = accountVerificationTokenService;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
        _passwordHasher = new PasswordHasher<Company>(); // Initialize PasswordHasher
    }

    public async Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request)
    {
        // Validate Organisation Number
        if (!Regex.IsMatch(request.OrganisationNumber, @"^\d{10}$"))
        {
            throw new InvalidOperationException("Organisation number must contain exactly 10 digits.");
        }

        // Validate Email format
        if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            throw new InvalidOperationException("Invalid email format.");
        }

        // Check for email or organisation number uniqueness (if already registered)
        if (await _companyRepository.CompanyExistsAsync(request.OrganisationNumber, request.Email))
        {
            throw new InvalidOperationException("Company with the provided Organisation Number or Email already exists.");
        }

        // Validate Business Type
        if (!Enum.IsDefined(typeof(BusinessType), request.BusinessType))
        {
            throw new InvalidOperationException("Invalid Business Type.");
        }

        // Validate Responsible Person's Name length
        if (request.ResponsiblePersonName.Length > 100)
        {
            throw new InvalidOperationException("Responsible person's name should not exceed 100 characters.");
        }

        // Validate Phone Number (Swedish phone number format)
        if (!Regex.IsMatch(request.PhoneNumber, @"^\+46\d{9}$"))
        {
            throw new InvalidOperationException("Phone number must be a valid Swedish number starting with +46.");
        }

        // Check if Terms and Conditions are accepted
        if (!request.TermsAndConditions)
        {
            throw new InvalidOperationException("You must accept the Terms and Conditions.");
        }

        // Validate Password strength
        var passwordRegex = new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$");
        if (!passwordRegex.IsMatch(request.Password))
        {
            throw new InvalidOperationException("Password must contain at least one letter, one number, and one special character.");
        }

        // Validate Password Confirmation
        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        // Hash the password before saving
        string hashedPassword = _passwordHasher.HashPassword(null, request.Password); // Use null for the object parameter

        // Create new company object
        var company = new Company
        {
            OrganisationNumber = request.OrganisationNumber,
            CompanyName = request.CompanyName,
            Email = request.Email,
            BusinessType = request.BusinessType,  // Assign directly as an enum
            ResponsiblePersonName = request.ResponsiblePersonName,
            PhoneNumber = request.PhoneNumber,
            TermsAndConditions = request.TermsAndConditions,
            IsVerified = false, // Initially false
            PasswordHash = hashedPassword // Save the hashed password
        };

        // Add company to the repository (database)
        await _companyRepository.AddAsync(company);

        // Generate email verification token
        var token = await _accountVerificationTokenService.GenerateVerificationTokenAsync(company.Id);

        // Call the EmailVerificationService to send the email
        bool emailSent = await _emailVerificationService.SendVerificationEmailAsync(token);

        if (!emailSent)
        {
            throw new InvalidOperationException("Failed to send verification email.");
        }

        // Return the response with success and token
        return new SignUpResponse
        {
            Success = true,
            CompanyId = company.Id,
            Token = token
        };
    }
}
