using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByIdAsync(Guid id);
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId);
        Task AddAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken refreshToken);
        void Delete(RefreshToken token);
        Task<RefreshToken> FindActiveTokenByPreviousTokenAsync(string previousToken, DateTime currentDateTime, int gracePeriodSeconds);
        Task<int> DeleteExpiredTokensAsync(int daysToKeep, int batchSize = 1000, int maxRetentionDays = 90);
        Task<bool> ExistsAsync(string token);
        Task RevokeAllForUserAsync(Guid userId);
    }
}
