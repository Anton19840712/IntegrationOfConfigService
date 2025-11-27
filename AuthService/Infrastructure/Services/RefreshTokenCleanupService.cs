using Application.Interfaces.Repository;
using Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RefreshTokenCleanupSettings _settings;

        public RefreshTokenCleanupService(
            ILogger<RefreshTokenCleanupService> logger, 
            IServiceProvider serviceProvider,
            IOptions<RefreshTokenCleanupSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Сервис очистки refresh-токенов отключен.");
                return;
            }

            _logger.LogInformation("Сервис очистки refresh-токенов запущен. Интервал: {IntervalHours}ч, Хранение: {DaysToKeep} дней", 
                _settings.CleanupIntervalHours, _settings.DaysToKeep);

            var cleanupInterval = TimeSpan.FromHours(_settings.CleanupIntervalHours);

            // Первый запуск через 10 секунд после старта приложения
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var refreshTokenRepo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                        
                        _logger.LogInformation("Начинаю очистку устаревших refresh-токенов...");
                        
                        var deletedCount = await refreshTokenRepo.DeleteExpiredTokensAsync(
                            _settings.DaysToKeep, 
                            _settings.BatchSize,
                            _settings.MaxRetentionDays);
                        
                        if (deletedCount > 0)
                        {
                            _logger.LogInformation("Успешно удалено {Count} устаревших refresh-токенов", deletedCount);
                        }
                        else
                        {
                            _logger.LogInformation("Не найдено устаревших refresh-токенов для удаления");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Ошибка при очистке refresh-токенов");
                }

                _logger.LogInformation("Следующая очистка через {Hours} часов", _settings.CleanupIntervalHours);
                await Task.Delay(cleanupInterval, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сервис очистки refresh-токенов останавливается.");
            await base.StopAsync(cancellationToken);
        }
    }
}