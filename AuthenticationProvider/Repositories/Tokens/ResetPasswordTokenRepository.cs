using AuthenticationProvider.Data;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Repositories.Tokens;

public class ResetPasswordTokenRepository : IResetPasswordTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResetPasswordTokenRepository> _logger;

    public ResetPasswordTokenRepository(ApplicationDbContext context, ILogger<ResetPasswordTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a reset password token by its ID.
    /// </summary>
    /// <param name="id">The ID of the token to retrieve.</param>
    /// <returns>A ResetPasswordTokenEntity if found, otherwise null.</returns>
    public async Task<ResetPasswordTokenEntity> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Id == id && t.ExpiryDate > DateTime.UtcNow && !t.IsUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving reset password token by ID.");
            throw new Exception("Det gick inte att hämta lösenordsåterställningstoken."); // User-friendly message in Swedish
        }
    }

    /// <summary>
    /// Retrieves a reset password token by the token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <returns>A ResetPasswordTokenEntity if found, otherwise null.</returns>
    public async Task<ResetPasswordTokenEntity> GetByTokenAsync(string token)
    {
        try
        {
            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow && !t.IsUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving reset password token by token.");
            throw new Exception("Det gick inte att hämta lösenordsåterställningstoken."); // User-friendly message in Swedish
        }
    }

    /// <summary>
    /// Creates a new reset password token and saves it to the database.
    /// </summary>
    /// <param name="token">The ResetPasswordTokenEntity to create.</param>
    /// <returns>The created ResetPasswordTokenEntity.</returns>
    public async Task<ResetPasswordTokenEntity> CreateAsync(ResetPasswordTokenEntity token)
    {
        try
        {
            _context.ResetPasswordTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating reset password token.");
            throw new Exception("Det gick inte att skapa lösenordsåterställningstoken."); // User-friendly message in Swedish
        }
    }

    /// <summary>
    /// Deletes all reset password tokens associated with a given company ID.
    /// </summary>
    /// <param name="companyId">The ID of the company for which tokens should be deleted.</param>
    public async Task DeleteAsync(Guid companyId)
    {
        try
        {
            var tokens = await _context.ResetPasswordTokens
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            if (tokens.Any())
            {
                _context.ResetPasswordTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting reset password tokens for company ID {CompanyId}.", companyId);
            throw new Exception("Det gick inte att ta bort lösenordsåterställningstoken."); // User-friendly message in Swedish
        }
    }

    /// <summary>
    /// Marks a reset password token as used by its token ID.
    /// </summary>
    /// <param name="tokenId">The ID of the token to mark as used.</param>
    public async Task MarkAsUsedAsync(Guid tokenId)
    {
        try
        {
            var token = await _context.ResetPasswordTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking reset password token as used for token ID {TokenId}.", tokenId);
            throw new Exception("Det gick inte att markera lösenordsåterställningstoken som använd."); // User-friendly message in Swedish
        }
    }
}
