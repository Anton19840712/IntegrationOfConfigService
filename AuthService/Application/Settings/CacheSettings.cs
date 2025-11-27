namespace Application.Settings;

/// <summary>
/// Настройки кеша для AuthService
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Включен ли User Cache (Tarantool)
    /// </summary>
    public bool UserCacheEnabled { get; set; } = true;

    /// <summary>
    /// TTL для User Cache в секундах (по умолчанию 15 минут)
    /// </summary>
    public int UserCacheTtlSeconds { get; set; } = 900;

    /// <summary>
    /// Включен ли Login Rate Limiting (Tarantool)
    /// </summary>
    public bool LoginRateLimitingEnabled { get; set; } = true;

    /// <summary>
    /// Максимальное количество неудачных попыток логина
    /// </summary>
    public int LoginRateLimitMaxAttempts { get; set; } = 5;

    /// <summary>
    /// Временное окно для отслеживания попыток (в секундах)
    /// По умолчанию 300 секунд = 5 минут
    /// </summary>
    public int LoginRateLimitWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Продолжительность блокировки после превышения лимита (в секундах)
    /// По умолчанию 900 секунд = 15 минут
    /// </summary>
    public int LoginRateLimitBlockDurationSeconds { get; set; } = 900;
}
