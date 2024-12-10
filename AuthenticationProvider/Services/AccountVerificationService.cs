using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services
{
    public class AccountVerificationService : IAccountVerificationService
    {
        private readonly AccountVerificationClient _accountVerificationClient;
        private readonly IAccountVerificationTokenService _accountVerificationTokenService;
        private readonly ITokenRevocationService _tokenRevocationService;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<AccountVerificationService> _logger;

        public AccountVerificationService(
            AccountVerificationClient accountVerificationClient,
            IAccountVerificationTokenService accountVerificationTokenService,
            ITokenRevocationService tokenRevocationService,
            ICompanyRepository companyRepository,
            ILogger<AccountVerificationService> logger)
        {
            _accountVerificationClient = accountVerificationClient;
            _accountVerificationTokenService = accountVerificationTokenService;
            _tokenRevocationService = tokenRevocationService;
            _companyRepository = companyRepository;
            _logger = logger;
        }

        public async Task<bool> SendVerificationEmailAsync(string token)
        {
            try
            {
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

        public async Task<VerificationResult> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for account verification.");
                return VerificationResult.InvalidToken;
            }

            var claimsPrincipal = _accountVerificationTokenService.ValidateAccountVerificationTokenAsync(token);
            if (claimsPrincipal == null)
            {
                _logger.LogWarning("Invalid token: {Token}", token);
                return VerificationResult.InvalidToken;
            }

            var email = ExtractEmailFromToken(token);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email not found in token: {Token}", token);
                return VerificationResult.EmailNotFound;
            }

            var company = await _companyRepository.GetByEmailAsync(email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", email);
                return VerificationResult.CompanyNotFound;
            }

            if (company.IsVerified)
            {
                _logger.LogInformation("The company is already verified.");
                return VerificationResult.AlreadyVerified;
            }

            company.IsVerified = true;
            await _companyRepository.UpdateAsync(company);

            await _tokenRevocationService.RevokeTokenAsync(token);

            _logger.LogInformation("Account verified successfully for company: {CompanyId}", company.Id);
            return VerificationResult.Success;
        }

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
