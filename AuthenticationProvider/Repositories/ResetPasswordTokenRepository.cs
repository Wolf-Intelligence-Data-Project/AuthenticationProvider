using AuthenticationProvider.Data;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationProvider.Repositories
{
    public class ResetPasswordTokenRepository : IResetPasswordTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public ResetPasswordTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ResetPasswordTokenEntity> GetByIdAsync(Guid id)
        {
            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Id == id && t.ExpiryDate > DateTime.UtcNow && !t.IsUsed);
        }
        public async Task<ResetPasswordTokenEntity> GetByTokenAsync(string token)
        {
            return await _context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow && !t.IsUsed);
        }

        public async Task<ResetPasswordTokenEntity> CreateAsync(ResetPasswordTokenEntity token)
        {
            _context.ResetPasswordTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task DeleteAsync(Guid companyId)
        {
            var tokens = await _context.ResetPasswordTokens
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            _context.ResetPasswordTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsUsedAsync(Guid tokenId)
        {
            var token = await _context.ResetPasswordTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
