using System.ComponentModel.DataAnnotations;

namespace ConfigurationService.Domain;

/// <summary>
/// Domain модель доступного SIP номера из пула
/// </summary>
public class AvailableSipAccount
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// SIP номер (например, 2001, 2004)
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Пароль для SIP регистрации
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string SipPassword { get; set; } = string.Empty;

    /// <summary>
    /// Назначен ли номер пользователю
    /// </summary>
    public bool IsAssigned { get; set; } = false;

    /// <summary>
    /// Дата назначения
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Дата добавления в пул
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
