using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthenticationProvider.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly IConfiguration _configuration = configuration;

    // Generate a token (used for both login session and email verification)
    public string GenerateToken(string email, string tokenType)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, email),  // Storing email as 'sub' claim
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Unique ID for the token
        new Claim("TokenType", tokenType)  // Custom claim to differentiate token types
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddHours(1),  // Expiration for the token
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
            return principal;
        }
        catch
        {
            return null; // Token validation failed, return null
        }
    }
}