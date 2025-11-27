using Application.DTOs;
using Application.Interfaces.Repository;
using Domain.Entities;

namespace Application.Services
{
    public class AuditLogService(IAuditLogRepository auditRepo)
	{
        private readonly IAuditLogRepository _auditRepo = auditRepo;

		public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> SearchLogsAsync(
            Guid? userId, string userLogin, string action, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            return await _auditRepo.SearchAsync(userId, userLogin, action, fromDate, toDate, page, pageSize);
        }

        public async Task<IEnumerable<string>> GetActionTypesAsync()
        {
            return await _auditRepo.GetDistinctActionsAsync();
        }

        public async Task<IEnumerable<SuspiciousActivityDto>> FindSuspiciousActivityAsync()
        {
            var suspiciousActivities = new List<SuspiciousActivityDto>();

            // Правило 1: Множественные неудачные попытки входа для одного логина за короткое время
            var (Logs, TotalCount) = await _auditRepo.SearchAsync(action: "LOGIN_FAILED", fromDate: DateTime.UtcNow.AddHours(-1));
            
            var groupedFailedLogins = Logs
                .GroupBy(l => l.UserLogin)
                .Where(g => g.Count() >= 5) // Порог: 5+ неудачных попыток за час
                .Select(g => new SuspiciousActivityDto
                {
                    ActivityType = "Multiple Failed Logins",
                    Description = $"User '{g.Key}' failed to log in {g.Count()} times in the last hour.",
                    InvolvedLogin = g.Key,
                    LastActivityAt = g.Max(l => l.Timestamp)
                });
            
            suspiciousActivities.AddRange(groupedFailedLogins);

            // Правило 2: Вход с разных IP-адресов за короткий промежуток времени
            var successfulLogins = await _auditRepo.SearchAsync(action: "LOGIN_SUCCESS", fromDate: DateTime.UtcNow.AddDays(-1));

            var loginsFromMultipleIps = successfulLogins.Logs
                .GroupBy(l => l.UserId)
                .Where(g => g.Select(l => l.IpAddress).Distinct().Count() > 3) // Порог: вход с 3+ разных IP за сутки
                .Select(g => new SuspiciousActivityDto
                {
                    ActivityType = "Login From Multiple IPs",
                    Description = $"User '{g.First().UserLogin}' logged in from {g.Select(l => l.IpAddress).Distinct().Count()} different IP addresses in the last 24 hours.",
                    InvolvedLogin = g.First().UserLogin,
                    InvolvedUserId = g.Key,
                    LastActivityAt = g.Max(l => l.Timestamp)
                });

            suspiciousActivities.AddRange(loginsFromMultipleIps);
            
            return suspiciousActivities.OrderByDescending(s => s.LastActivityAt);
        }
    }
}