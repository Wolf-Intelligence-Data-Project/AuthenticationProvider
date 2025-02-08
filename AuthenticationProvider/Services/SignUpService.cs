using AuthenticationProvider.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using AuthenticationProvider.Models.Responses;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Services;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Interfaces.Services.Tokens;

namespace AuthenticationProvider.Services;

/// <summary>
/// Service responsible for handling the sign-up process.
/// </summary>
public class SignUpService : ISignUpService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService;
    private readonly IAddressRepository _addressRepository;
    private readonly ILogger<SignUpService> _logger;
    private readonly PasswordHasher<CompanyEntity> _passwordHasher;
    private readonly IEmailRestrictionService _emailRestrictionService;

    public SignUpService(
        ICompanyRepository companyRepository,
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationService accountVerificationService,
        IAddressRepository addressRepository,
        ILogger<SignUpService> logger,
        IEmailRestrictionService emailRestrictionService)
    {
        _companyRepository = companyRepository;
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService;
        _addressRepository = addressRepository;
        _logger = logger;
        _passwordHasher = new PasswordHasher<CompanyEntity>();
        _emailRestrictionService = emailRestrictionService;
    }

    public async Task<SignUpResponse> RegisterCompanyAsync(SignUpRequest request)
    {
        ValidateSignUpRequest(request);

        // Check if the email is restricted before proceeding
        if (_emailRestrictionService.IsRestrictedEmail(request.Email))
        {
            throw new InvalidOperationException("Den angivna e-posten är inte tillåten.");
        }

        if (await _companyRepository.CompanyExistsAsync(request.OrganizationNumber, request.Email))
        {
            throw new InvalidOperationException("Ett företag med angivet organisationsnummer eller e-post finns redan.");
        }

        // Hash the password
        string hashedPassword = _passwordHasher.HashPassword(null, request.Password);

        var company = new CompanyEntity
        {
            OrganizationNumber = request.OrganizationNumber,
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
        if (emailSent != ServiceResult.Success)
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

        await _companyRepository.DeleteAsync(companyId);
        _logger.LogInformation($"Företaget med ID {companyId} har raderats.");
    }

    private async Task AddAddressesAsync(SignUpRequest request, CompanyEntity company)
    {
        // Retrieve all existing addresses associated with the company once
        var existingAddresses = await _addressRepository.GetAddressesByCompanyIdAsync(company.Id);

        // Add primary address
        if (request.PrimaryAddress != null)
        {
            if (string.IsNullOrWhiteSpace(request.PrimaryAddress.StreetAddress) ||
                string.IsNullOrWhiteSpace(request.PrimaryAddress.City) ||
                string.IsNullOrWhiteSpace(request.PrimaryAddress.PostalCode))
            {
                throw new InvalidOperationException("Primary address is incomplete.");
            }

            // Check if the primary address already exists for the company
            if (existingAddresses.Any(a => a.StreetAddress == request.PrimaryAddress.StreetAddress &&
                                           a.City == request.PrimaryAddress.City &&
                                           a.PostalCode == request.PrimaryAddress.PostalCode))
            {
                throw new InvalidOperationException("Den här adressen finns redan för företaget.");
            }

            var primaryAddress = new AddressEntity
            {
                StreetAddress = request.PrimaryAddress.StreetAddress,
                City = request.PrimaryAddress.City,
                PostalCode = request.PrimaryAddress.PostalCode,
                CompanyId = company.Id,
                Region = request.PrimaryAddress.Region,
                IsPrimary = true
            };

            await _addressRepository.AddAsync(primaryAddress);
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

                // Check if the additional address already exists for the company
                if (existingAddresses.Any(a => a.StreetAddress == additionalAddress.StreetAddress &&
                                               a.City == additionalAddress.City &&
                                               a.PostalCode == additionalAddress.PostalCode))
                {
                    throw new InvalidOperationException("Den här adressen finns redan för företaget.");
                }

                var address = new AddressEntity
                {
                    StreetAddress = additionalAddress.StreetAddress,
                    City = additionalAddress.City,
                    PostalCode = additionalAddress.PostalCode,
                    CompanyId = company.Id,
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
