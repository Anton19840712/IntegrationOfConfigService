using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserLoggedOutEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public string IpAddress { get; init; } = null!;
    }
}