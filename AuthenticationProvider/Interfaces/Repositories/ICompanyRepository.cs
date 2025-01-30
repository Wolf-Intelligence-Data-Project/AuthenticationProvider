using AuthenticationProvider.Models.Data.Entities;

namespace AuthenticationProvider.Interfaces.Repositories;

public interface ICompanyRepository
{
    Task<bool> CompanyExistsAsync(string organisationNumber, string email);
    Task AddAsync(CompanyEntity company);
    Task UpdateAsync(CompanyEntity company);
    Task<CompanyEntity> GetByEmailAsync(string email);
    Task<CompanyEntity> GetByIdAsync(Guid companyId);
    Task DeleteAsync(Guid companyId);
}