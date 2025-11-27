using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace ConfigurationService.Events;

/// <summary>
/// Событие создания pending assignment (пользователь добавлен в очередь ожидания)
/// Публикуется в exchange "configservice.events" и обрабатывается NotificationService
/// для отправки уведомления администратору о добавлении пользователя в очередь
/// </summary>
public class SipAccountPendingCreated : IntegrationEvent
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
    /// Отображаемое имя пользователя
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Позиция в очереди
    /// </summary>
    public int QueuePosition { get; set; }

    /// <summary>
    /// Время создания pending assignment
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Время события
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Сообщение для администратора
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
