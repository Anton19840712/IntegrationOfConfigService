using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserRoleChangeEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public List<Guid> NewRoleIds { get; set; }
        public List<Guid> RemoveRoleIds { get; set; }
    }
}