using System.ComponentModel.DataAnnotations;

namespace ConfigurationService.Domain;

/// <summary>
/// Domain модель пользователя ожидающего назначения SIP номера
/// </summary>
public class PendingAssignment
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID пользователя из AuthService
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Login пользователя (для удобства)
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string UserLogin { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Статус ожидания
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "WaitingForAvailableAccount";

    /// <summary>
    /// Дата создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
