using Application.Interfaces.Repository;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// PostgreSQL реализация репозитория RefreshTokens через Entity Framework Core
    /// </summary>
    public class PostgresRefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
    {
        private readonly AuthDbContext _db = db;

		public async Task<RefreshToken> GetByIdAsync(Guid id) =>
            await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == id);

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId) =>
            await _db.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
        }


        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _db.RefreshTokens.Update(refreshToken);
            await _db.SaveChangesAsync();
        }


        public void Delete(RefreshToken token)
        {
            _db.RefreshTokens.Remove(token);
            _db.SaveChanges();
        }
        
        public async Task<bool> ExistsAsync(string token)
        {
            return await _db.RefreshTokens.AnyAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken> FindActiveTokenByPreviousTokenAsync(string previousToken, DateTime currentDateTime, int gracePeriodSeconds)
        {
            var gracePeriodStart = currentDateTime.AddSeconds(-gracePeriodSeconds);

            var result = await _db.RefreshTokens
                .Where(rt => rt.ReplacedByToken == previousToken &&
                            rt.RevokedAt == null &&
                            rt.CreatedAt >= gracePeriodStart)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();
            return result;
        }
        
        public async Task<int> DeleteExpiredTokensAsync(int daysToKeep, int batchSize = 1000, int maxRetentionDays = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var maxCutoffDate = DateTime.UtcNow.AddDays(-maxRetentionDays);
            
            // Безопасное удаление пачками для больших таблиц
            var tokensToDelete = await _db.RefreshTokens
                .Where(rt => (rt.CreatedAt < cutoffDate ||
                             (rt.RevokedAt != null && rt.RevokedAt < cutoffDate) ||
                             (rt.Expires < DateTime.UtcNow)) &&
                             rt.CreatedAt >= maxCutoffDate) // Защита от случайного удаления всего
                .OrderBy(rt => rt.CreatedAt) // Удаляем сначала старые
                .Take(batchSize)
                .ToListAsync();

            if (tokensToDelete.Count == 0)
                return 0;

            _db.RefreshTokens.RemoveRange(tokensToDelete);
            return await _db.SaveChangesAsync();
        }

        public async Task RevokeAllForUserAsync(Guid userId)
        {
            var tokens = await _db.RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null).ToListAsync();
            foreach (var t in tokens)
                t.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
