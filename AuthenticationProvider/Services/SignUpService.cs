using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using AuthenticationProvider.Data.Dtos;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;

namespace AuthenticationProvider.Services
{
    public class SignUpService : ISignUpService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAccountVerificationTokenService _accountVerificationTokenService;
        private readonly IAccountVerificationService _accountVerificationService;
        private readonly IAddressRepository _addressRepository;
        private readonly ILogger<SignUpService> _logger;
        private readonly PasswordHasher<CompanyEntity> _passwordHasher;

        public SignUpService(
            ICompanyRepository companyRepository,
            IAccountVerificationTokenService accountVerificationTokenService,
            IAccountVerificationService accountVerificationService,
            IAddressRepository addressRepository,
            ILogger<SignUpService> logger)
        {
            _companyRepository = companyRepository;
            _accountVerificationTokenService = accountVerificationTokenService;
            _accountVerificationService = accountVerificationService;
            _addressRepository = addressRepository;
            _logger = logger;
            _passwordHasher = new PasswordHasher<CompanyEntity>();
        }

        public async Task<SignUpResponse> RegisterCompanyAsync(SignUpDto request)
        {
            // Validation Logic
            ValidateSignUpRequest(request);

            // Check if company already exists
            if (await _companyRepository.CompanyExistsAsync(request.OrganisationNumber, request.Email))
            {
                throw new InvalidOperationException("Ett företag med angivet organisationsnummer eller e-post finns redan.");
            }

            // Hash the password
            string hashedPassword = _passwordHasher.HashPassword(null, request.Password);

            // Create new company object
            var company = new CompanyEntity
            {
                OrganisationNumber = request.OrganisationNumber,
                CompanyName = request.CompanyName,
                Email = request.Email,
                BusinessType = request.BusinessType,
                ResponsiblePersonName = request.ResponsiblePersonName,
                PhoneNumber = request.PhoneNumber,
                TermsAndConditions = request.TermsAndConditions,
                IsVerified = false,
                PasswordHash = hashedPassword
            };

            // Add the company to the database
            await _companyRepository.AddAsync(company);

            // Add the primary and additional addresses
            await AddAddressesAsync(request, company);

            // Generate an account verification token
            var token = await _accountVerificationTokenService.CreateAccountVerificationTokenAsync(company.Id);

            // Send the verification email
            var emailSent = await _accountVerificationService.SendVerificationEmailAsync(token);
            if (!emailSent)
            {
                throw new InvalidOperationException("Det gick inte att skicka verifieringsmail.");
            }

            return new SignUpResponse
            {
                Success = true,
                CompanyId = company.Id,
                Token = token
            };
        }

        public async Task DeleteCompanyAsync(Guid companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                throw new InvalidOperationException("Företaget hittades inte.");
            }

            // Delete the company
            await _companyRepository.DeleteAsync(companyId);
            _logger.LogInformation($"Företaget med ID {companyId} har raderats.");
        }

        private async Task AddAddressesAsync(SignUpDto request, CompanyEntity company)
        {
            _logger.LogInformation("Adding primary address for company {CompanyId}.", company.Id);

            // Add primary address
            if (request.PrimaryAddress != null)
            {
                if (string.IsNullOrWhiteSpace(request.PrimaryAddress.StreetAddress) ||
                    string.IsNullOrWhiteSpace(request.PrimaryAddress.City) ||
                    string.IsNullOrWhiteSpace(request.PrimaryAddress.PostalCode))
                {
                    throw new InvalidOperationException("Primary address is incomplete.");
                }

                var primaryAddress = new AddressEntity
                {
                    StreetAddress = request.PrimaryAddress.StreetAddress,
                    City = request.PrimaryAddress.City,
                    PostalCode = request.PrimaryAddress.PostalCode,
                    CompanyId = company.Id,
                    AddressType = "Primary",
                    Region = request.PrimaryAddress.Region,
                    IsPrimary = true
                };

                await _addressRepository.AddAsync(primaryAddress);
                _logger.LogInformation("Primary address added for company {CompanyId}.", company.Id);
            }

            // Add additional addresses
            if (request.AdditionalAddresses != null)
            {
                foreach (var additionalAddress in request.AdditionalAddresses)
                {
                    if (string.IsNullOrWhiteSpace(additionalAddress.StreetAddress) ||
                        string.IsNullOrWhiteSpace(additionalAddress.City) ||
                        string.IsNullOrWhiteSpace(additionalAddress.PostalCode))
                    {
                        throw new InvalidOperationException("Additional address is incomplete.");
                    }

                    var address = new AddressEntity
                    {
                        StreetAddress = additionalAddress.StreetAddress,
                        City = additionalAddress.City,
                        PostalCode = additionalAddress.PostalCode,
                        CompanyId = company.Id,
                        AddressType = "Additional",
                        Region = additionalAddress.Region,
                        IsPrimary = false
                    };

                    await _addressRepository.AddAsync(address);
                    _logger.LogInformation("Additional address added for company {CompanyId}.", company.Id);
                }
            }
        }

        private void ValidateSignUpRequest(SignUpDto request)
        {
            // Organisation Number (10 digits)
            if (!Regex.IsMatch(request.OrganisationNumber, @"^\d{10}$"))
            {
                throw new InvalidOperationException("Organisationsnumret måste innehålla exakt 10 siffror.");
            }

            // Email format
            if (!new EmailAddressAttribute().IsValid(request.Email))
            {
                throw new InvalidOperationException("Ogiltigt e-postformat.");
            }

            // Password strength
            var passwordRegex = new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+={}\[\]:;'<>?\/.,]).{8,}$");
            if (!passwordRegex.IsMatch(request.Password))
            {
                throw new InvalidOperationException("Lösenordet måste innehålla minst en bokstav, en siffra och ett specialtecken.");
            }

            // Password confirmation
            if (request.Password != request.ConfirmPassword)
            {
                throw new InvalidOperationException("Lösenorden matchar inte.");
            }

            // Business Type validation
            if (!Enum.IsDefined(typeof(BusinessType), request.BusinessType))
            {
                throw new InvalidOperationException("Ogiltig företagstyp.");
            }

            // Responsible person's name
            if (request.ResponsiblePersonName.Length > 100)
            {
                throw new InvalidOperationException("Ansvarig persons namn får inte överstiga 100 tecken.");
            }

            // Phone Number (Swedish format)
            if (!Regex.IsMatch(request.PhoneNumber, @"^\+46\d{9}$"))
            {
                throw new InvalidOperationException("Telefonnumret måste vara ett giltigt svenskt nummer som börjar med +46.");
            }

            // Terms and Conditions
            if (!request.TermsAndConditions)
            {
                throw new InvalidOperationException("Du måste acceptera villkoren.");
            }
        }
    }
}
