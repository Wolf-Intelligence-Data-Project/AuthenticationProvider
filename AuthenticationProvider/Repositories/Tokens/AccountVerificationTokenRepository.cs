using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Repositories.Tokens;

/// Repository for handling operations related to account verification tokens.
public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<AccountVerificationTokenRepository> _logger;

    public AccountVerificationTokenRepository(UserDbContext context, ILogger<AccountVerificationTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new account verification token and saves it to the database.
    /// </summary>
    /// <param name="token">The token entity to be created.</param>
    /// <returns>The created token entity.</returns>
    public async Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token)
    {
        try
        {
            await _context.AccountVerificationTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Retrieves an un-used account verification token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <returns>The found token entity, or null if not found.</returns>
    public async Task<AccountVerificationTokenEntity> GetByTokenAsync(string token)
    {
        try
        {
            return await _context.AccountVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Retrieves an account verification token by its ID.
    /// </summary>
    /// <param name="tokenId">The token's unique ID.</param>
    /// <returns>The found token entity, or null if not found.</returns>
    public async Task<AccountVerificationTokenEntity> GetTokenByIdAsync(Guid tokenId)
    {
        try
        {
            return await _context.AccountVerificationTokens
                                 .FirstOrDefaultAsync(t => t.Id == tokenId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Marks the specified token as used.
    /// </summary>
    /// <param name="tokenId">The token's unique ID.</param>
    public async Task MarkAsUsedAsync(Guid tokenId)
    {
        try
        {
            var token = await _context.AccountVerificationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Token with ID {TokenId} not found.", tokenId);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Revokes and deletes all account verification tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique ID.</param>
    public async Task RevokeAndDeleteAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(c => c.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("User not found.");
                return;
            }

            if (!user.IsVerified)
            {
                return;
            }

            var tokens = await _context.AccountVerificationTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    token.IsUsed = true;
                }

                _context.AccountVerificationTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("No tokens found.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Fel vid att återkalla och ta bort verifieringstoken.", ex);
        }
    }

    /// <summary>
    /// Revokes and deletes a specific account verification token by its token string.
    /// </summary>
    /// <param name="token">The token string to be revoked and deleted.</param>
    public async Task RevokeAndDeleteByTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for revocation.");
                return;  // Just return as no action is needed
            }

            // Retrieve the token from the database
            var tokenEntity = await _context.AccountVerificationTokens
                                             .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);
            if (tokenEntity == null)
            {
                _logger.LogWarning("Token not found or already used.");
                return;
            }

            // Mark the token as used and delete it
            tokenEntity.IsUsed = true;
            _context.AccountVerificationTokens.Remove(tokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token has been revoked and deleted.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }
}
