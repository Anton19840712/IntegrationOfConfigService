using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserLoggedInEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public string UserLogin { get; init; } = null!;
        public string IpAddress { get; init; } = null!;
        public DateTime TimestampUtc { get; set; }
        public string UserAgent { get; set; }
    }
}