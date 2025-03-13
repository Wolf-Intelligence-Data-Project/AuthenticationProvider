using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Repositories.Tokens;

public class ResetPasswordTokenRepository : IResetPasswordTokenRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<ResetPasswordTokenRepository> _logger;

    public ResetPasswordTokenRepository(UserDbContext context, ILogger<ResetPasswordTokenRepository> logger)
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
            var stockholmTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));
            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Id == id && t.ExpiryDate > stockholmTime && !t.IsUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving reset password token by ID.");
            throw new Exception("Det gick inte att hämta lösenordsåterställningstoken.");
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
            var stockholmTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm"));

            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > stockholmTime && !t.IsUsed);
        }
        catch (Exception ex)
        {
            throw new Exception("Det gick inte att återställa lösenordet.", ex);
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
            throw new Exception("Det gick inte återställa lösenordet.");
        }
    }

    /// <summary>
    /// Deletes all reset password tokens associated with a given user ID.
    /// </summary>
    /// <param name="userId">The ID of the user for which tokens should be deleted.</param>
    public async Task DeleteAsync(Guid userId)
    {
        try
        {
            var tokens = await _context.ResetPasswordTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (tokens.Any())
            {
                _context.ResetPasswordTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Det gick inte återställa lösenordet.");
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
            throw new Exception("Det gick inte återställa lösenordet.");
        }
    }
}
