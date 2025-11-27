namespace ConfigurationService.Models;

/// <summary>
/// Статус SIP аккаунта пользователя
/// </summary>
public class SipAccountStatusDto
{
    /// <summary>
    /// Статус назначения: assigned, pending, not_requested
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Логин пользователя
    /// </summary>
    public string UserLogin { get; set; } = null!;

    /// <summary>
    /// SIP аккаунт (если назначен)
    /// </summary>
    public SipAccountDto? SipAccount { get; set; }

    /// <summary>
    /// Позиция в очереди ожидания (если pending)
    /// </summary>
    public int? PendingPosition { get; set; }

    /// <summary>
    /// Время создания pending assignment (если pending)
    /// </summary>
    public DateTime? PendingCreatedAt { get; set; }

    /// <summary>
    /// Сообщение для пользователя
    /// </summary>
    public string? Message { get; set; }
}
