using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Services;
using Microsoft.Extensions.Logging;

public class SignUpService : ISignUpService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<SignUpService> _logger;

    public SignUpService(
        ICompanyRepository companyRepository,
        ITokenService tokenService,
        IEmailVerificationService emailVerificationService,
        ILogger<SignUpService> logger)
    {
        _companyRepository = companyRepository;
        _tokenService = tokenService;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
    }

    public async Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request)
    {
        if (await _companyRepository.CompanyExistsAsync(request.OrganisationNumber, request.Email))
        {
            _logger.LogWarning("Company with Organisation Number {OrganisationNumber} or Email {Email} already exists.", request.OrganisationNumber, request.Email);
            throw new InvalidOperationException("Company with the provided Organisation Number or Email already exists.");
        }

        var company = new Company
        {
            OrganisationNumber = request.OrganisationNumber,
            CompanyName = request.CompanyName,
            Email = request.Email,
            BusinessType = request.BusinessType,
            ResponsiblePersonName = request.ResponsiblePersonName,
            PhoneNumber = request.PhoneNumber,
            TermsAndConditions = request.TermsAndConditions,
            IsVerified = false // Initially false
        };

        await _companyRepository.AddAsync(company);

        // Generate email verification token
        var token = _tokenService.GenerateToken(company.Email, TokenType.EmailVerification.ToString());

        // Log the generated token
        _logger.LogInformation("Generated email verification token for company {Email}: {Token}", company.Email, token);

        // Call the EmailVerificationProvider to send the email
        bool emailSent = await _emailVerificationService.SendVerificationEmailAsync(request.Email, token);

        if (!emailSent)
        {
            _logger.LogError("Failed to send verification email to {Email}.", request.Email);
            throw new InvalidOperationException("Failed to send verification email.");
        }

        _logger.LogInformation("Verification email successfully sent to {Email}.", request.Email);

        return new SignUpResponse
        {
            Success = true,
            CompanyId = company.Id,
            Token = token
        };
    }
}
