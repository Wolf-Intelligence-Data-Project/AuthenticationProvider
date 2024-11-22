using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class SignUpService : ISignUpService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAddressRepository _addressRepository; // Declare address repository
        private readonly ITokenProvider _tokenService;  
        private readonly IEmailVerificationProvider _emailVerificationService;

        // Constructor initializes the repositories
        public SignUpService(
            ICompanyRepository companyRepository,
            IAddressRepository addressRepository,  // Inject address repository
            ITokenProvider tokenService,
            IEmailVerificationProvider emailVerificationService)
        {
            _companyRepository = companyRepository;
            _addressRepository = addressRepository;  // Assign address repository to the private field
            _tokenService = tokenService;
            _emailVerificationService = emailVerificationService;
        }

        public async Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request)
        {
            // Check if the company already exists by Organisation Number or Email
            if (await _companyRepository.CompanyExistsAsync(request.OrganisationNumber, request.Email))
            {
                throw new InvalidOperationException("Company with the provided Organisation Number or Email already exists.");
            }

            // Create the company
            var company = new Company
            {
                OrganisationNumber = request.OrganisationNumber,
                CompanyName = request.CompanyName,
                Email = request.Email,
                BusinessType = request.BusinessType,
                ResponsiblePersonName = request.ResponsiblePersonName,
                PhoneNumber = request.PhoneNumber,
                TermsAndConditions = request.TermsAndConditions,
                PrimaryAddress = new Address // You should also assign the address based on the request
                {
                    StreetAddress = request.StreetAddress,
                    PostalCode = request.PostalCode,
                    City = request.City,
                    Region = request.Region
                }
            };

            // Add the company to the repository (this could also be done in a transaction to keep things atomic)
            await _companyRepository.AddAsync(company);

            // Generate the email verification token
            var token = await _tokenService.GenerateTokenAsync(request.Email, TokenType.EmailVerification);

            // Send verification email with token via Email Verification Service
            await _emailVerificationService.SendVerificationEmailAsync(request.Email, token);

            // Return the SignUpResponse with a success flag, company info, and token
            return new SignUpResponse
            {
                Success = true,
                CompanyId = company.Id,
                Token = token
            };
        }
    }
}
