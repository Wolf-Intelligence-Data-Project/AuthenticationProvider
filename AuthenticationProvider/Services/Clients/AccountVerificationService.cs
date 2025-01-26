using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Responses;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
            _configuration = configuration;
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
                    _logger.LogError("Failed to send email.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email");
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
                return VerificationResult.InvalidToken;
            }

            // Extract the email from the token
            var email = ExtractEmailFromToken(token);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email not found.");
                return VerificationResult.EmailNotFound;
            }

            // Retrieve the company using the provided email
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found.");
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
                return VerificationResult.InvalidToken;
            }

            _logger.LogInformation("Account verified successfully.");
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
                    ClockSkew = TimeSpan.Zero
                };

                // Validate the token
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Ensure the token is a valid JWT
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    return claimsPrincipal;
                }
                return null;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token has expired.");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while validating account.");
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
                var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == "email");
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
                // Perform the revocation action (no result expected from the repository method)
                await _accountVerificationTokenRepository.RevokeAndDeleteByTokenAsync(token);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
