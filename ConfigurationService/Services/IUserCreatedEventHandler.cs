using ConfigurationService.Events;
using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace ConfigurationService.Services;

/// <summary>
/// Обработчик события создания пользователя из AuthService
/// </summary>
public interface IUserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
}
