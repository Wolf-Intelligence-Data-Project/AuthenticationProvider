using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services.Tokens;

public class AccountVerificationTokenService : IAccountVerificationTokenService
{
    private readonly IAccountVerificationTokenRepository _accountVerificationTokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountVerificationTokenService> _logger;

    public AccountVerificationTokenService(
        IAccountVerificationTokenRepository accountVerificationTokenRepository,
        ICompanyRepository companyRepository,
        IConfiguration configuration,
        ILogger<AccountVerificationTokenService> logger)
    {
        _accountVerificationTokenRepository = accountVerificationTokenRepository;
        _companyRepository = companyRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateAccountVerificationTokenAsync(Guid companyId)
    {

        // Check if the company exists
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
        {
            _logger.LogWarning("Company not found: {CompanyId}", companyId);
            throw new ArgumentException("Invalid company ID.");
        }

        // Check if the company is already verified
        if (company.IsVerified)
        {
            _logger.LogWarning("Account already verified for company: {CompanyId}", companyId);
            throw new InvalidOperationException("The company account is already verified. No further verification tokens can be generated.");
        }


        if (string.IsNullOrEmpty(company.Email))
        {
            _logger.LogWarning("Company email is null or empty: {CompanyId}", companyId);
            throw new ArgumentException("Company email is required to generate account verification token.");
        }

        // Check for existing tokens for the company
        var existingToken = await _accountVerificationTokenRepository.GetTokenByIdAsync(companyId);
        if (existingToken != null)
        {
            // Revoke and delete the existing token
            _logger.LogInformation("Existing token found for company {CompanyId}. Revoking and deleting the token.", companyId);
            await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);
        }

        // Generate a new JWT token
        var secretKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"]; // Add the audience from configuration

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new ArgumentNullException("JWT settings are missing in the configuration.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, companyId.ToString()),
            new Claim(ClaimTypes.Email, company.Email),                
            new Claim("token_type", "AccountVerification"),          
        }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience, 
            SigningCredentials = credentials
        };

        var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(jwtToken);

        // Save the new token in the database
        var accountVerificationToken = new AccountVerificationTokenEntity
        {
            Token = tokenString,
            CompanyId = companyId,
            ExpiryDate = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        await _accountVerificationTokenRepository.CreateAsync(accountVerificationToken);

        _logger.LogInformation("Account verification token created for company: {CompanyId}", companyId);

        return tokenString;
    }



    public async Task<ClaimsPrincipal> ValidateAccountVerificationTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException("Token is required for validation.");
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new ArgumentNullException("JWT settings are missing in the configuration.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,  // Ensures token is not expired
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = securityKey,  // Ensures the token is signed with the correct key
                ClockSkew = TimeSpan.Zero // Optional: to avoid a small clock skew
            };

            // Validate the token and extract claims
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Check if token type is correct for account verification
            var jwtToken = validatedToken as JwtSecurityToken;
            var tokenTypeClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

            if (tokenTypeClaim != "AccountVerification")
            {
                throw new SecurityTokenException("Invalid token type.");
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Account verification token has expired.");
            throw new UnauthorizedAccessException("Token has expired.");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError("Invalid token: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Invalid token.");
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while validating token: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Token validation failed.");
        }
    }


    public async Task MarkAccountVerificationTokenAsUsedAsync(string token)
    {
        var storedToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);
        if (storedToken != null)
        {
            storedToken.IsUsed = true;
            await _accountVerificationTokenRepository.MarkAsUsedAsync(storedToken.Id);
            _logger.LogInformation("Account verification token marked as used: {TokenId}", storedToken.Id);
        }
        else
        {
            _logger.LogWarning("Attempted to mark a non-existent token as used: {Token}", token);
        }
    }

    public async Task<AccountVerificationTokenEntity> GetValidAccountVerificationTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("The provided token is invalid or empty.");
            return null;
        }

        var accountVerificationToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);

        if (accountVerificationToken == null)
        {
            _logger.LogWarning("The provided account verification token does not exist.");
            return null;
        }

        if (accountVerificationToken.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning($"The account verification token has expired. Token: {token}");
            return null;
        }

        if (accountVerificationToken.IsUsed)
        {
            _logger.LogWarning($"The account verification token has already been used. Token: {token}");
            return null;
        }

        return accountVerificationToken;
    }

    public async Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId)
    {
        // Calling the new RevokeAndDeleteAsync method from the repository
        await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);
        _logger.LogInformation("All account verification tokens for company {CompanyId} have been revoked and deleted.", companyId);
    }
}
