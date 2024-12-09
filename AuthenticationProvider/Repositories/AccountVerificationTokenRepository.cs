using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using Microsoft.EntityFrameworkCore; // Added this directive

namespace AuthenticationProvider.Repositories
{
    public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountVerificationTokenRepository(ApplicationDbContext context)
        {
            _context = context;
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

        // New DeleteAsync method
        public async Task DeleteAsync(Guid companyId)
        {
            var tokens = await _context.AccountVerificationTokens
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            if (tokens.Any())
            {
                _context.AccountVerificationTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
            }
        }
    }
}
