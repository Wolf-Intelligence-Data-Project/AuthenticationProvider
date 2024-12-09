using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Generate a token for email verification
    public string GenerateToken(string email, string tokenType)
    {
        if (tokenType != "EmailVerification")
        {
            throw new ArgumentException("Invalid token type. Only 'EmailVerification' is supported.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),  // Storing email as 'sub' claim
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Unique ID for the token
            new Claim("TokenType", tokenType)  // Custom claim to indicate token type
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationTime = DateTime.Now.AddDays(7);  // Email verification token expires in 7 days

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Issuer"],
            claims,
            expires: expirationTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);  // Return the token to the client
    }

    // Validate a token and return the ClaimsPrincipal
    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // Remove delay of token when expired
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Ensure the token is of type EmailVerification
            var tokenTypeClaim = principal.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
            if (tokenTypeClaim != "EmailVerification")
            {
                throw new SecurityTokenException("Invalid token type.");
            }

            return principal;
        }
        catch
        {
            return null; // Token validation failed, return null
        }
    }
}
