using Application.Settings;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class RefreshTokenCleanupHealthCheck : IHealthCheck
    {
        private readonly RefreshTokenCleanupSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public RefreshTokenCleanupHealthCheck(
            IOptions<RefreshTokenCleanupSettings> settings,
            IServiceProvider serviceProvider)
        {
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Проверяем, включен ли сервис
                if (!_settings.Enabled)
                {
                    return HealthCheckResult.Degraded(
                        "Сервис очистки токенов отключен в настройках",
                        data: new Dictionary<string, object>
                        {
                            { "Enabled", false },
                            { "IntervalHours", _settings.CleanupIntervalHours }
                        });
                }

                // 2. Проверяем подключение к базе данных
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                    
                    // Простой запрос для проверки соединения
                    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                    
                    if (!canConnect)
                    {
                        return HealthCheckResult.Unhealthy(
                            "Нет подключения к базе данных",
                            data: new Dictionary<string, object>
                            {
                                { "Database", "Not connected" },
                                { "Service", "Disabled due to DB issue" }
                            });
                    }

                    // 3. Проверяем, есть ли токены для очистки
                    var tokensCount = await dbContext.RefreshTokens
                        .Where(rt => rt.CreatedAt < DateTime.UtcNow.AddDays(-_settings.DaysToKeep) ||
                                    (rt.RevokedAt != null && rt.RevokedAt < DateTime.UtcNow.AddDays(-_settings.DaysToKeep)) ||
                                    rt.Expires < DateTime.UtcNow)
                        .CountAsync(cancellationToken);

                    // 4. Возвращаем результат с полезной информацией
                    return HealthCheckResult.Healthy(
                        "Сервис очистки токенов активен и подключен к БД",
                        data: new Dictionary<string, object>
                        {
                            { "Enabled", true },
                            { "IntervalHours", _settings.CleanupIntervalHours },
                            { "DaysToKeep", _settings.DaysToKeep },
                            { "PendingTokensForCleanup", tokensCount },
                            { "LastCheck", DateTime.UtcNow }
                        });
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Ошибка при проверке состояния сервиса очистки токенов",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "Error", ex.Message },
                        { "ErrorType", ex.GetType().Name },
                        { "Timestamp", DateTime.UtcNow }
                    });
            }
        }
    }
}