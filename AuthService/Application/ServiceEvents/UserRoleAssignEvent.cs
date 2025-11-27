using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserRoleRemoveEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public Guid RemoveRoleId { get; set; }
    }
}