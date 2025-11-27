using System.ComponentModel.DataAnnotations;

namespace ConfigurationService.Domain;

/// <summary>
/// Domain модель SIP аккаунта пользователя
/// </summary>
public class SipAccount
{
    /// <summary>
    /// Уникальный идентификатор конфигурации
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID пользователя (связь с AuthService)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SIP username для регистрации на Asterisk
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// SIP пароль
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string SipPassword { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя пользователя для SIP (используется в Janus)
    /// </summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// SIP домен (например, asterisk-server.local)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string SipDomain { get; set; } = string.Empty;

    /// <summary>
    /// URI прокси сервера (например, sip:asterisk-server.local:5060)
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string ProxyUri { get; set; } = string.Empty;

    /// <summary>
    /// Активна ли конфигурация
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
