using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace Application.ServiceEvents
{
    public class UserUpdatedEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
        public string UserLogin { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
    }
}