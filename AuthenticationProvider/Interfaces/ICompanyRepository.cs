using AuthenticationProvider.Models;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces;

public interface ICompanyRepository
{
    /// <summary>
    /// Checks if a company exists based on organisation number and email.
    /// </summary>
    /// <param name="organisationNumber">The organisation number of the company.</param>
    /// <param name="email">The email of the company.</param>
    /// <returns>True if the company exists, otherwise false.</returns>
    Task<bool> CompanyExistsAsync(string organisationNumber, string email);

    /// <summary>
    /// Adds a new company to the database.
    /// </summary>
    /// <param name="company">The company to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Company company);

    /// <summary>
    /// Updates an existing company's details in the database.
    /// </summary>
    /// <param name="company">The company to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Company company);

    /// <summary>
    /// Retrieves a company by its email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The company if found, otherwise null.</returns>
    Task<Company> GetByEmailAsync(string email);

    Task<Company> GetByIdAsync(Guid companyId);
}
