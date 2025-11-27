using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace ConfigurationService.Events;

/// <summary>
/// Событие истощения пула SIP номеров
/// </summary>
public class SipAccountPoolDepletedEvent : IntegrationEvent
{
    /// <summary>
    /// ID пользователя, для которого не нашлось номера
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Login пользователя
    /// </summary>
    public string UserLogin { get; set; } = string.Empty;

    /// <summary>
    /// Время события
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Сообщение
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
