using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Repositories.Tokens;

/// Repository for handling operations related to email verification tokens.
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly UserDbContext _userDbcontext;
    private readonly ILogger<EmailVerificationTokenRepository> _logger;

    public EmailVerificationTokenRepository(UserDbContext context, ILogger<EmailVerificationTokenRepository> logger)
    {
        _userDbcontext = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new email verification token and saves it to the database.
    /// </summary>
    /// <param name="token">The token entity to be created.</param>
    /// <returns>The created token entity.</returns>
    public async Task<EmailVerificationTokenEntity> CreateAsync(EmailVerificationTokenEntity token)
    {
        try
        {
            await _userDbcontext.EmailVerificationTokens.AddAsync(token);
            await _userDbcontext.SaveChangesAsync();
            return token;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Retrieves an un-used email verification token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <returns>The found token entity, or null if not found.</returns>
    public async Task<EmailVerificationTokenEntity> GetByTokenAsync(string token)
    {
        try
        {
            return await _userDbcontext.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }

    /// <summary>
    /// Retrieves an email verification token by its ID.
    /// </summary>
    /// <param name="tokenId">The token's unique ID.</param>
    /// <returns>The found token entity, or null if not found.</returns>
    public async Task<EmailVerificationTokenEntity> GetByIdAsync(Guid tokenId)
    {
        try
        {
            return await _userDbcontext.EmailVerificationTokens
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
            var token = await _userDbcontext.EmailVerificationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _userDbcontext.SaveChangesAsync();
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
    /// Revokes and deletes all email verification tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique ID.</param>
    public async Task RevokeAndDeleteAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Looking for user with UserId: {UserId}", userId);
            var user = await _userDbcontext.Set<UserEntity>().FirstOrDefaultAsync(c => c.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("User not found.");
                return;
            }

            if (user.IsVerified)
            {
                return;
            }

            var tokens = await _userDbcontext.EmailVerificationTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    token.IsUsed = true;
                }

                _userDbcontext.EmailVerificationTokens.RemoveRange(tokens);
                await _userDbcontext.SaveChangesAsync();
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
    /// Revokes and deletes a specific email verification token by its token string.
    /// </summary>
    /// <param name="token">The token string to be revoked and deleted.</param>
    public async Task RevokeAndDeleteByTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for revocation.");
                return;
            }

            var tokenEntity = await _userDbcontext.EmailVerificationTokens
                                             .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);
            if (tokenEntity == null)
            {
                _logger.LogWarning("Token not found or already used.");
                return;
            }

            tokenEntity.IsUsed = true;
            _userDbcontext.EmailVerificationTokens.Remove(tokenEntity);
            await _userDbcontext.SaveChangesAsync();

            _logger.LogInformation("Token has been revoked and deleted.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Det gick inte verifiera kontot.", ex);
        }
    }
}
