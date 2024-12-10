using AuthenticationProvider.Models;

namespace AuthenticationProvider.Interfaces;

public interface ICompanyRepository
{
    Task<bool> CompanyExistsAsync(string organisationNumber, string email);
    Task AddAsync(Company company);
    Task UpdateAsync(Company company);
    Task<Company> GetByEmailAsync(string email);
    Task<Company> GetByIdAsync(Guid companyId);
    Task DeleteAsync(Guid companyId);
}