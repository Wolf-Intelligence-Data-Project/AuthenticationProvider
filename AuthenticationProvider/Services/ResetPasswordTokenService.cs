using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Repositories;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class ResetPasswordTokenService : IResetPasswordTokenService
    {
        private readonly IResetPasswordTokenRepository _resetpasswordtokenRepository;
        private readonly ICompanyRepository _companyRepository;

        public ResetPasswordTokenService(IResetPasswordTokenRepository resetpasswordtokenRepository, ICompanyRepository companyRepository)
        {
            _resetpasswordtokenRepository = resetpasswordtokenRepository;
            _companyRepository = companyRepository;
        }

        public async Task<ResetPasswordToken> CreateResetPasswordTokenAsync(Guid companyId)
        {
            var generatedToken = Guid.NewGuid().ToString();
            var tokenExpiryDate = DateTime.UtcNow.AddHours(1); // 1 hour expiry

            var resetpasswordtoken = new ResetPasswordToken
            {
                CompanyId = companyId,
                Token = generatedToken,
                ExpiryDate = tokenExpiryDate,
                IsUsed = false
            };

            return await _resetpasswordtokenRepository.CreateAsync(resetpasswordtoken);
        }

        public async Task<ResetPasswordToken> GetValidResetPasswordTokenAsync(string token)
        {
            return await _resetpasswordtokenRepository.GetByTokenAsync(token);
        }

        public async Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId)
        {
            await _resetpasswordtokenRepository.MarkAsUsedAsync(tokenId);
        }

        public async Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId)
        {
            await _resetpasswordtokenRepository.DeleteAsync(companyId);
        }

        public async Task<bool> ResetCompanyPasswordAsync(string email, string newPassword)
        {
            // Retrieve the company by email
            var company = await _companyRepository.GetByEmailAsync(email);

            if (company == null)
            {
                return false; // Company not found
            }

            // Hash the new password
            var hashedPassword = HashPassword(newPassword);

            // Update the company's password
            company.PasswordHash = hashedPassword;

            await _companyRepository.UpdateAsync(company);
            return true;
        }

        // Password hashing method (e.g., bcrypt or PBKDF2)
        private string HashPassword(string password)
        {
            // Example: Implement password hashing (e.g., using bcrypt)
            // You can use a library like BCrypt.Net-Next or PBKDF2 to hash the password.
            return password; // Placeholder: Replace this with actual hashing logic.
        }
    }
}
