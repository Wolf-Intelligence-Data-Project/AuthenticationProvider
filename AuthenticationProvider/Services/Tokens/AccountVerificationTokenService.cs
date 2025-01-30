using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Models.Data.Entities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Responses.Errors;
using Microsoft.AspNetCore.Mvc;
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
            _logger.LogWarning("The company not found");
            throw new ArgumentException("Ogiltigt företag.");
        }

        // Check if the company is already verified
        if (company.IsVerified)
        {
            _logger.LogWarning("The account is already verified.");
            throw new InvalidOperationException("Företagskontot är redan verifierat.");
        }

        if (string.IsNullOrEmpty(company.Email))
        {
            _logger.LogWarning("Company email cannot be null.");
            throw new ArgumentException("E-post krävs för att verifiera kontot.");
        }

        // Check for existing tokens for the company
        var existingToken = await _accountVerificationTokenRepository.GetTokenByIdAsync(companyId);
        if (existingToken != null)
        {
            await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);
        }

        // Generate a new JWT token
        var secretKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new ArgumentNullException("JWT-inställningar saknas i konfigurationen."); 
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

        // Save the new token in the database (account verification token and reset password token are saved as their own entities)
        var accountVerificationToken = new AccountVerificationTokenEntity
        {
            Token = tokenString,
            CompanyId = companyId,
            ExpiryDate = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        await _accountVerificationTokenRepository.CreateAsync(accountVerificationToken);

        return tokenString;
    }

    public async Task<IActionResult> ValidateAccountVerificationTokenAsync([FromBody] TokenRequest request)
    {
        if (string.IsNullOrEmpty(request?.Token))
        {
            _logger.LogWarning("Token is null or empty.");
            return new BadRequestObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT settings are missing from configuration.");
                throw new ArgumentNullException("JWT settings are missing from configuration.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero // no clock skew allowed
            };

            // Validate the token and extract claims
            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

            // Check if token type is correct for account verification
            var jwtToken = validatedToken as JwtSecurityToken;
            var tokenTypeClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

            if (tokenTypeClaim != "AccountVerification")
            {
                _logger.LogWarning("Invalid token type: {TokenType}", tokenTypeClaim);
                return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
            }

            return new OkObjectResult(principal); // Token is valid
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Account verification token has expired.");
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError("Invalid token: {Message}", ex.Message);
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while validating token: {Message}", ex.Message);
            return new UnauthorizedObjectResult(ErrorResponses.TokenExpiredOrInvalid);
        }
    }


    public async Task MarkAccountVerificationTokenAsUsedAsync(string token)
    {
        var storedToken = await _accountVerificationTokenRepository.GetByTokenAsync(token);
        if (storedToken != null)
        {
            storedToken.IsUsed = true;
            await _accountVerificationTokenRepository.MarkAsUsedAsync(storedToken.Id);
        }
        else
        {
            _logger.LogWarning("The token does not exist.");
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
            _logger.LogWarning("The token does not exist.");
            return null;
        }

        if (accountVerificationToken.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning($"The token has expired.");
            return null;
        }

        if (accountVerificationToken.IsUsed)
        {
            return null;
        }

        return accountVerificationToken;
    }

    public async Task DeleteAccountVerificationTokensForCompanyAsync(Guid companyId)
    {
        await _accountVerificationTokenRepository.RevokeAndDeleteAsync(companyId);
    }

}
