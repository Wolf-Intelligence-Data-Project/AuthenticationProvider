using AuthenticationProvider.Data;
using AuthenticationProvider.Data.Entities;
using AuthenticationProvider.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthenticationProvider.Repositories
{
    public class AccountVerificationTokenRepository : IAccountVerificationTokenRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountVerificationTokenRepository> _logger;

        public AccountVerificationTokenRepository(ApplicationDbContext context, ILogger<AccountVerificationTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AccountVerificationTokenEntity> CreateAsync(AccountVerificationTokenEntity token)
        {
            await _context.AccountVerificationTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<AccountVerificationTokenEntity> GetByTokenAsync(string token)
        {
            return await _context.AccountVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);
        }

        public async Task<AccountVerificationTokenEntity> GetTokenByIdAsync(Guid tokenId)
        {
            return await _context.AccountVerificationTokens
                                 .FirstOrDefaultAsync(t => t.Id == tokenId);
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

        public async Task RevokeAndDeleteAsync(Guid companyId)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                _logger.LogWarning("Company with ID {CompanyId} not found.", companyId);
                return;
            }

            if (!company.IsVerified)
            {
                _logger.LogInformation("Company with ID {CompanyId} is not verified. Tokens will not be revoked or deleted.", companyId);
                return;
            }

            var tokens = await _context.AccountVerificationTokens
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    token.IsUsed = true;
                }

                await _context.SaveChangesAsync();

                _context.AccountVerificationTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("All account verification tokens for company {CompanyId} have been revoked and deleted.", companyId);
            }
            else
            {
                _logger.LogWarning("No tokens found for company {CompanyId} to revoke and delete.", companyId);
            }
        }
    }
}
