using AuthenticationProvider.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Interfaces.Services
{
    public interface IResetPasswordTokenService
    {
        // Method to create a reset password token for a company identified by email.
        Task<string> CreateResetPasswordTokenAsync(string email);

        // Method to get a valid reset password token from the repository.
        Task<ResetPasswordTokenEntity> GetValidResetPasswordTokenAsync(string token);

        // Method to extract the email from the reset password token.
        Task<string> GetEmailFromTokenAsync(string token);

        // Method to reset the company's password by email and new password.
        Task<bool> ResetCompanyPasswordAsync(string email, string newPassword);

        // Method to mark the reset password token as used after successful password reset.
        Task MarkResetPasswordTokenAsUsedAsync(Guid tokenId);
        Task DeleteResetPasswordTokensForCompanyAsync(Guid companyId);

        Task<bool> ValidateResetPasswordTokenAsync(string token);
    }
}
