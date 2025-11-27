using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace ConfigurationService.Events;

/// <summary>
/// Событие создания пользователя из AuthService
/// </summary>
public class UserCreatedEvent : IntegrationEvent
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Login пользователя
    /// </summary>
    public string UserLogin { get; set; } = string.Empty;

    /// <summary>
    /// Email пользователя
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Имя
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Фамилия
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Отчество
    /// </summary>
    public string? MiddleName { get; set; }
}
