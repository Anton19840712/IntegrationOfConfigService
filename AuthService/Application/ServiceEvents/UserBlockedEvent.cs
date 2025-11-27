using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserBlockedEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public string UserLogin { get; init; } = null!;
    }
}