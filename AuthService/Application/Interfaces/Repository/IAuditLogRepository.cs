using Domain.Entities;

namespace Application.Interfaces.Repository
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog logEntry);
        Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int take = 50);
        Task<(IEnumerable<AuditLog> Logs, int TotalCount)> SearchAsync(
            Guid? userId = null,
            string userLogin = null,
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20);          
        Task<IEnumerable<string>> GetDistinctActionsAsync();
    }
}
