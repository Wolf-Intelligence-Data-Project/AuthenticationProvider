using AuthenticationProvider.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationProvider.Services;

public class AccessTokenService : IAccessTokenService  // Implementing the IAccessTokenService interface
{
    private readonly Dictionary<string, string> _tokens = new();
    private readonly IConfiguration _configuration;

    public AccessTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(IdentityUser user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        AddToken(user.Id, tokenString);

        return tokenString;
    }

    private void AddToken(string userId, string token)
    {
        _tokens[userId] = token;
    }

    public string GetToken(string userId)
    {
        _tokens.TryGetValue(userId, out var token);
        return token;
    }

    public void RevokeAccessToken(string userId)
    {
        RemoveToken(userId);
    }

    private void RemoveToken(string userId)
    {
        _tokens.Remove(userId);
    }
}
