using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Services.Security.Clients;

namespace AuthenticationProvider.Services.Security;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IResetPasswordClient _resetPasswordClient;
    private readonly IResetPasswordTokenService _resetPasswordTokenService;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<ResetPasswordService> _logger;
    private readonly PasswordHasher<object> _passwordHasher;

    public ResetPasswordService(
        IResetPasswordClient resetPasswordClient,
        IResetPasswordTokenService resetPasswordTokenService,
        ICompanyRepository companyRepository,
        ILogger<ResetPasswordService> logger)
    {
        _resetPasswordClient = resetPasswordClient ?? throw new ArgumentNullException(nameof(resetPasswordClient));
        _resetPasswordTokenService = resetPasswordTokenService ?? throw new ArgumentNullException(nameof(resetPasswordTokenService));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = new PasswordHasher<object>();
    }

    /// <summary>
    /// Sends a reset password email using the provided token.
    /// </summary>
    /// <param name="token">The reset password token.</param>
    /// <returns>True if the email is sent successfully; otherwise, false.</returns>
    public async Task<bool> SendResetPasswordEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 10)
        {
            _logger.LogWarning("Invalid token provided for reset password email.");
            return false;
        }

        try
        {
            if (!await EmailValidation(token))
            {
                _logger.LogWarning("The email in the token does not match any company.");
                return false;
            }

            // Validate the token
            var resetPasswordToken = await _resetPasswordTokenService.ValidateResetPasswordTokenAsync(token);
            if (!resetPasswordToken)
            {
                _logger.LogWarning("Reset password token is invalid or expired.");
                return false;
            }

            // Send the reset password email using the client
            bool result = await _resetPasswordClient.SendResetPasswordEmailAsync(token);

            if (result)
            {
                _logger.LogInformation("Reset password email sent successfully.");
            }
            else
            {
                _logger.LogWarning("Failed to send reset password email.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the reset password email.");
            return false;
        }
    }

    /// <summary>
    /// Handles the actual password reset when the user submits the new password.
    /// </summary>
    /// <param name="resetPasswordRequest">The request containing the token and the new password.</param>
    /// <returns>True if the password is successfully reset; otherwise, false.</returns>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
    {
        if (string.IsNullOrWhiteSpace(resetPasswordRequest.Token) ||
            string.IsNullOrWhiteSpace(resetPasswordRequest.NewPassword) ||
            string.IsNullOrWhiteSpace(resetPasswordRequest.ConfirmPassword))
        {
            _logger.LogWarning("Invalid reset password request.");
            return false;
        }

        if (resetPasswordRequest.NewPassword != resetPasswordRequest.ConfirmPassword)
        {
            _logger.LogWarning("New password and confirm password do not match.");
            return false;
        }

        try
        {

            // Validate the reset password token
            var resetPasswordToken = await _resetPasswordTokenService.GetValidResetPasswordTokenAsync(resetPasswordRequest.Token);
            if (resetPasswordToken == null)
            {
                _logger.LogWarning("Reset password token is invalid or expired.");
                return false;
            }

            // Additional validation logic
            if (resetPasswordToken.IsUsed)
            {
                _logger.LogWarning("The reset password token has already been used.");
                return false;
            }

            // Update the company's password hash
            var company = await _companyRepository.GetByIdAsync(resetPasswordToken.CompanyId);
            if (company != null)
            {
                company.PasswordHash = _passwordHasher.HashPassword(null, resetPasswordRequest.NewPassword);
                await _companyRepository.UpdateAsync(company);
            }
            else
            {
                _logger.LogWarning("Company not found for the provided token.");
                return false;
            }

            // Mark the token as used
            await _resetPasswordTokenService.MarkResetPasswordTokenAsUsedAsync(resetPasswordToken.Id);

            _logger.LogInformation("Password reset successfully.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resetting the password.");
            return false;
        }
    }
    private async Task<bool> EmailValidation(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Log all claims for debugging
            foreach (var claim in jwtToken.Claims)
            {
                _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            }

            // Extract the email claim from the token
            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(emailClaim))
            {
                _logger.LogWarning("No email claim found in the token.");
                return false;
            }

            // Check if the email exists in the company database
            var company = await _companyRepository.GetByEmailAsync(emailClaim);
            if (company == null)
            {
                _logger.LogWarning("No company found with the email from the token.");
                return false;
            }

            // Compare the email from the token with the company's email
            if (!company.Email.Equals(emailClaim, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("The email in the token does not match the company's email.");
                return false;
            }

            // Check token expiration
            if (jwtToken.ValidTo < TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")))
            {
                _logger.LogWarning("The token has expired.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating email from token.");
            return false;
        }
    }

}
