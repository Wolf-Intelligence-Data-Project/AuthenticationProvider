using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.SignUp;
using AuthenticationProvider.Models.Tokens;

namespace AuthenticationProvider.Services;

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

        // Call the EmailVerificationProvider to send the email
        bool emailSent = await _emailVerificationService.SendVerificationEmailAsync(token);

        if (!emailSent)
        {
            throw new InvalidOperationException("Failed to send verification email.");
        }

        return new SignUpResponse
        {
            Success = true,
            CompanyId = company.Id,
            Token = token
        };
    }
}