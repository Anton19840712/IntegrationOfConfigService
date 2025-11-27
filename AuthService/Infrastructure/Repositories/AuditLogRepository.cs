using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Application.Interfaces.Repository;

namespace Infrastructure.Repositories
{
    public class AuditLogRepository(AuthDbContext context, IUserRepository userRepo, IHttpContextAccessor httpContextAccessor, IpAddressHelper ipHelper) : IAuditLogRepository
    {
        private readonly AuthDbContext _context = context;
        private readonly IUserRepository _userRepo = userRepo;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IpAddressHelper _ipHelper = ipHelper;

		public async Task AddAsync(AuditLog logEntry)
        {
            if (string.IsNullOrEmpty(logEntry.IpAddress))
            {
                logEntry.IpAddress = _ipHelper.GetClientIpAddress();
            }

            // Уже есть UserId, но UserLogin не указан → попробуем получить из claims
            if (logEntry.UserId.HasValue && string.IsNullOrEmpty(logEntry.UserLogin))
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var loginClaim = httpContext.User.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.Name);
                    if (loginClaim != null)
                        logEntry.UserLogin = loginClaim.Value;
                }
            }

            if (string.IsNullOrEmpty(logEntry.UserLogin))
            {
                logEntry.UserLogin = "anonymous";
            }

            await _context.AuditLogs.AddAsync(logEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int take = 50)
        {
            return await _context.AuditLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(take)
                .ToListAsync();
        }
        
        public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> SearchAsync(
            Guid? userId = null,
            string userLogin = null,
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId.Value);
            }
            if (!string.IsNullOrEmpty(userLogin))
            {
                query = query.Where(l => l.UserLogin.Contains(userLogin));
            }
            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(l => l.Action == action);
            }
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var logs = await query.OrderByDescending(l => l.Timestamp)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<IEnumerable<string>> GetDistinctActionsAsync()
        {
            return await _context.AuditLogs
                                 .Select(l => l.Action)
                                 .Distinct()
                                 .OrderBy(a => a)
                                 .ToListAsync();
        }
    }
}
