using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly EmailVerificationClient _emailVerificationClient;
        private readonly ITokenService _tokenService;
        private readonly ITokenRevocationService _tokenRevocationService;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(
            EmailVerificationClient emailVerificationClient,
            ITokenService tokenService,
            ITokenRevocationService tokenRevocationService,
            ICompanyRepository companyRepository,
            ILogger<EmailVerificationService> logger)
        {
            _emailVerificationClient = emailVerificationClient;
            _tokenService = tokenService;
            _tokenRevocationService = tokenRevocationService;
            _companyRepository = companyRepository;
            _logger = logger;
        }

        // Sends the verification email
        public async Task<bool> SendVerificationEmailAsync(string token)
        {
            try
            {
                return await _emailVerificationClient.SendVerificationEmailAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email.");
                return false;
            }
        }

        // Verifies the email and updates the company status
        public async Task<VerificationResult> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for email verification.");
                return VerificationResult.InvalidToken;
            }

            // Validate the token
            var claimsPrincipal = _tokenService.ValidateToken(token);
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

            // Fetch company by email
            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return VerificationResult.CompanyNotFound;
            }

            // If the company is already verified, no need to update
            if (company.IsVerified)
            {
                _logger.LogInformation("The company is already verified.");
                return VerificationResult.AlreadyVerified;
            }

            // Mark the company as verified
            company.IsVerified = true;
            await _companyRepository.UpdateAsync(company);

            // Revoke the token once it is used
            await _tokenRevocationService.RevokeTokenAsync(token);

            _logger.LogInformation("Email verified successfully for company: {CompanyId}", company.Id);
            return VerificationResult.Success;
        }

        // Extract the email from the token
        private string ExtractEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var decodedToken = tokenHandler.ReadJwtToken(token);
                var emailClaim = decodedToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                return emailClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email from token.");
                return null;
            }
        }
    }

}
