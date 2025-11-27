namespace ConfigurationService.Models;

/// <summary>
/// DTO для отображения SIP аккаунта
/// </summary>
public class SipAccountDto
{
    /// <summary>
    /// ID конфигурации
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SIP username
    /// </summary>
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// SIP домен
    /// </summary>
    public string SipDomain { get; set; } = string.Empty;

    /// <summary>
    /// URI прокси
    /// </summary>
    public string ProxyUri { get; set; } = string.Empty;

    /// <summary>
    /// Активен ли аккаунт
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO для создания/обновления SIP аккаунта
/// </summary>
public class CreateUpdateSipAccountDto
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SIP username
    /// </summary>
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// SIP пароль
    /// </summary>
    public string SipPassword { get; set; } = string.Empty;

    /// <summary>
    /// SIP домен
    /// </summary>
    public string SipDomain { get; set; } = string.Empty;

    /// <summary>
    /// URI прокси
    /// </summary>
    public string ProxyUri { get; set; } = string.Empty;

    /// <summary>
    /// Транспорт (UDP/TCP/TLS)
    /// </summary>
    public string ProxyTransport { get; set; } = "UDP";

    /// <summary>
    /// TTL регистрации
    /// </summary>
    public int RegisterTtl { get; set; } = 3600;

    /// <summary>
    /// Активен ли аккаунт
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO для внутреннего использования (BFF) - включает пароль
/// </summary>
public class SipAccountWithPasswordDto
{
    /// <summary>
    /// ID конфигурации
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SIP username
    /// </summary>
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// SIP пароль
    /// </summary>
    public string SipPassword { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя пользователя
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// SIP домен
    /// </summary>
    public string SipDomain { get; set; } = string.Empty;

    /// <summary>
    /// URI прокси
    /// </summary>
    public string ProxyUri { get; set; } = string.Empty;

    /// <summary>
    /// Активен ли аккаунт
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO для агрегированного ответа: конфигурация пользователя + доступные номера для звонка
/// </summary>
public class SipAccountWithAvailableNumbersDto
{
    /// <summary>
    /// Конфигурация текущего пользователя (с паролем)
    /// </summary>
    public SipAccountWithPasswordDto CurrentAccount { get; set; } = new();

    /// <summary>
    /// Список доступных SIP номеров для звонка (кроме текущего пользователя)
    /// </summary>
    public List<string> AvailableNumbers { get; set; } = new();
}
