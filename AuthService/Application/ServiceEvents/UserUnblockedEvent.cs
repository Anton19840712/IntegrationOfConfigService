using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    /// <summary>
    /// Событие: пользователь разблокирован.
    /// </summary>
    public class UserUnblockedEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public string UserLogin { get; init; } = null!;
    }
}
