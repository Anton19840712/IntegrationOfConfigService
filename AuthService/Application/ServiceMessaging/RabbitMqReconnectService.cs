using SipIntegration.EventBus.RabbitMQ.Abstractions;

using Application.Interfaces.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.ServiceMessaging
{
    public class RabbitMqReconnectService : BackgroundService
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<RabbitMqReconnectService> _logger;

        public RabbitMqReconnectService(IEventBus eventBus, ILogger<RabbitMqReconnectService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис переподключения RabbitMQ запущен.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_eventBus.IsConnected)
                    {
                        _logger.LogDebug("Попытка переподключения к RabbitMQ...");
                        _eventBus.TryConnect();
                    }

                    if (_eventBus.IsConnected)
                    {
                        await _eventBus.PublishPendingEventsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка в сервисе переподключения RabbitMQ.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("Сервис переподключения RabbitMQ остановлен.");
        }
    }
}
