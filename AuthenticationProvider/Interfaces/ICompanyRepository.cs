using AuthenticationProvider.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface ICompanyRepository
{
    // Check if a company exists by organisation number and email
    Task<bool> CompanyExistsAsync(string organisationNumber, string email);

    // Add a new company
    Task AddAsync(Company company);

    // Update an existing company
    Task UpdateAsync(Company company);

    // Retrieve a company by email
    Task<Company> GetByEmailAsync(string email);

    // Retrieve a company by organisation number
    Task<Company> GetByOrganisationNumberAsync(string organisationNumber);

    // Retrieve a company by GUID (Id)
    Task<Company> GetByGuidAsync(Guid companyId);

    // Retrieve the last email verification token for a company
    Task<string> GetLastEmailVerificationTokenAsync(string email);

    // Update the email verification token for a company
    Task UpdateEmailVerificationTokenAsync(string email, string token);

    // Get a company for email verification
    Task<Company> GetCompanyForVerificationAsync(string email);

    // Revoke email verification token for a company
    Task RevokeEmailVerificationTokenAsync(string email);
}
