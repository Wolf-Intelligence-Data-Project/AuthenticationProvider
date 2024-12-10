using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;  // Make sure to include this namespace for logging

namespace AuthenticationProvider.Repositories
{
    public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountVerificationTokenRepository> _logger;

        // Inject ILogger in the constructor
        public AccountVerificationTokenRepository(ApplicationDbContext context, ILogger<AccountVerificationTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AccountVerificationToken> CreateAsync(AccountVerificationToken token)
        {
            await _context.AccountVerificationTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<AccountVerificationToken> GetByTokenAsync(string token)
        {
            return await _context.AccountVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed); // Asynchronous query
        }

        public async Task MarkAsUsedAsync(Guid tokenId)
        {
            var token = await _context.AccountVerificationTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }

        // New DeleteAndRevokeAsync method
        public async Task RevokeAndDeleteAsync(Guid companyId)
        {
            // Fetch tokens for the given company
            var tokens = await _context.AccountVerificationTokens
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            if (tokens.Any())
            {
                // First: Revoke each token (mark as used)
                foreach (var token in tokens)
                {
                    token.IsUsed = true;
                }

                // Save the changes to mark tokens as used (revoked)
                await _context.SaveChangesAsync();

                // Second: Now delete the tokens from the table
                _context.AccountVerificationTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();

                // Log the revocation and deletion
                _logger.LogInformation("All account verification tokens for company {CompanyId} have been revoked and deleted.", companyId);
            }
            else
            {
                _logger.LogWarning("No tokens found for company {CompanyId} to revoke and delete.", companyId);
            }
        }
    }
}
