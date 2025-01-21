using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Responses;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services.Clients
{

    /// Service for managing account verification (sending verification emails, verifying tokens, etc.).

    public class AccountVerificationService : IAccountVerificationService
    {
        private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
        private readonly IAccountVerificationClient _accountVerificationClient;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<AccountVerificationService> _logger;
        private readonly IConfiguration _configuration;


        /// Constructor for dependency injection.

        public AccountVerificationService(
            IAccountVerificationTokenRepository accountVerificationTokenRepository,
            IAccountVerificationClient accountVerificationClient,
            ICompanyRepository companyRepository,
            ILogger<AccountVerificationService> logger,
            IConfiguration configuration)
        {
            _accountVerificationTokenRepository = accountVerificationTokenRepository;
            _accountVerificationClient = accountVerificationClient;
            _companyRepository = companyRepository;
            _logger = logger;
            _configuration = configuration; // Initialize _configuration
        }


        /// Sends an account verification email.

        public async Task<bool> SendVerificationEmailAsync(string token)
        {
            try
            {
                // Attempt to send the email using the account verification client
                var result = await _accountVerificationClient.SendVerificationEmailAsync(token);
                if (!result)
                {
                    _logger.LogError("Failed to send email for token: {Token}", token);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email for token: {Token}", token);
                return false;
            }
        }


        /// Verifies the account using the provided token.

        public async Task<VerificationResult> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for account verification.");
                return VerificationResult.InvalidToken;
            }

            // Validate the token and extract the claims principal
            var claimsPrincipal = ValidateAccountVerificationToken(token);
            if (claimsPrincipal == null)
            {
                _logger.LogWarning("Invalid token: {Token}", token);
                return VerificationResult.InvalidToken;
            }

            // Extract the email from the token
            var email = ExtractEmailFromToken(token);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email not found in token: {Token}", token);
                return VerificationResult.EmailNotFound;
            }

            // Retrieve the company using the provided email
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return VerificationResult.CompanyNotFound;
            }

            // If the company is already verified, return early
            if (company.IsVerified)
            {
                _logger.LogInformation("The company is already verified.");
                return VerificationResult.AlreadyVerified;
            }

            // Set the company as verified
            company.IsVerified = true;
            await _companyRepository.UpdateAsync(company);

            // Revoke and delete the token after successful verification
            var revokeResult = await RevokeTokenAsync(token);
            if (!revokeResult)
            {
                _logger.LogError("Failed to revoke and delete tokens for company: {CompanyId}", company.Id);
                return VerificationResult.InvalidToken;
            }

            _logger.LogInformation("Account verified successfully for company: {CompanyId}", company.Id);
            return VerificationResult.Success;
        }

        /// Validates the account verification token.

        private ClaimsPrincipal ValidateAccountVerificationToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                // Set token validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Optional: Adjust for more precise expiration validation
                };

                // Validate the token
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Ensure the token is a valid JWT
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    return claimsPrincipal;
                }

                _logger.LogWarning("Invalid JWT structure for token: {Token}", token);
                return null;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token has expired: {Message}", ex.Message);
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while validating account verification token.");
                return null;
            }
        }

        /// Extracts the email from the verification token.

        private string ExtractEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var decodedToken = tokenHandler.ReadJwtToken(token);
                var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == "email");  // Ensure 'email' claim is used
                return emailClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email from token.");
                return null;
            }
        }

        /// Revokes and deletes the account verification token.
        private async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                _logger.LogInformation("Revoking and deleting token: {Token}", token);

                // Perform the revocation action (no result expected from the repository method)
                await _accountVerificationTokenRepository.RevokeAndDeleteByTokenAsync(token);

                _logger.LogInformation("Token successfully revoked and deleted: {Token}", token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while revoking and deleting token: {Token}", token);
                return false;
            }
        }
    }
}
