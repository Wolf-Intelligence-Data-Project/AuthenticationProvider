using AuthenticationProvider.Models;

namespace AuthenticationProvider.Interfaces;

public interface ICompanyRepository
{
    Task<bool> CompanyExistsAsync(string organisationNumber, string email);
    Task AddAsync(Company company);
}
