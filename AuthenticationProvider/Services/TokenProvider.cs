using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace AuthenticationProvider.Services;

public class TokenProvider : ITokenProvider
{
    private readonly string _secretKey;

    public TokenProvider()
    {
        // Your secret key should be kept in configuration for production
        _secretKey = "your_secret_key_here"; // Replace with your actual secret key
    }

    public async Task<string> GenerateTokenAsync(string email, TokenType tokenType)
    {
        // Set expiration times for different token types
        var expirationTime = tokenType switch
        {
            TokenType.EmailVerification => DateTime.UtcNow.AddHours(1),  // 1 hour expiration for email verification
            TokenType.LoginSession => DateTime.UtcNow.AddDays(7),  // 7 days expiration for login session
            _ => throw new InvalidOperationException("Unknown token type")
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email),  // Use the email as a placeholder for the user name
            new Claim("TokenType", tokenType.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: "your_issuer",  // Set to your actual issuer
            audience: "your_audience",  // Set to your actual audience
            claims: claims,
            expires: expirationTime,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(tokenDescriptor);

        return token;
    }
}
