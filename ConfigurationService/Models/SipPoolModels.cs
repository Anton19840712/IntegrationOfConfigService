namespace ConfigurationService.Models;

/// <summary>
/// DTO для добавления SIP номера в пул
/// </summary>
public class AddSipAccountDto
{
    /// <summary>
    /// SIP номер (например, 2007)
    /// </summary>
    public string SipAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Пароль для SIP регистрации
    /// </summary>
    public string SipPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO для bulk добавления SIP номеров
/// </summary>
public class BulkAddSipAccountsRequest
{
    /// <summary>
    /// Список SIP номеров для добавления
    /// </summary>
    public List<AddSipAccountDto> Accounts { get; set; } = new();
}

/// <summary>
/// Результат bulk добавления
/// </summary>
public class BulkAddSipAccountsResponse
{
    /// <summary>
    /// Количество успешно добавленных номеров
    /// </summary>
    public int AddedCount { get; set; }

    /// <summary>
    /// Количество пропущенных (уже существуют)
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Список пропущенных номеров
    /// </summary>
    public List<string> SkippedAccounts { get; set; } = new();

    /// <summary>
    /// Количество автоматически назначенных pending пользователям
    /// </summary>
    public int AutoAssignedCount { get; set; }
}

/// <summary>
/// Статистика пула SIP номеров
/// </summary>
public class SipPoolStatsResponse
{
    /// <summary>
    /// Всего номеров в пуле
    /// </summary>
    public int TotalAccounts { get; set; }

    /// <summary>
    /// Назначенных номеров
    /// </summary>
    public int AssignedAccounts { get; set; }

    /// <summary>
    /// Свободных номеров
    /// </summary>
    public int AvailableAccounts { get; set; }

    /// <summary>
    /// Пользователей в ожидании
    /// </summary>
    public int PendingAssignments { get; set; }
}

/// <summary>
/// Запрос на добавление pending assignment
/// </summary>
public class AddPendingAssignmentRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление SIP аккаунта
/// </summary>
public class UpdateSipAccountRequest
{
    public string? DisplayName { get; set; }
    public string? SipDomain { get; set; }
    public string? ProxyUri { get; set; }
    public string? UserId { get; set; }
}
