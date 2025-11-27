namespace Application.Interfaces.Service;

/// <summary>
/// Сервис для rate limiting попыток логина
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>
    /// Проверить, разрешен ли логин для данного пользователя/IP
    /// </summary>
    Task<RateLimitResult> CheckAsync(string loginOrIp);

    /// <summary>
    /// Увеличить счетчик неудачных попыток
    /// </summary>
    Task IncrementAsync(string loginOrIp);

    /// <summary>
    /// Сбросить счетчик (после успешного логина)
    /// </summary>
    Task ResetAsync(string loginOrIp);
}

/// <summary>
/// Результат проверки rate limiting
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Разрешен ли вход
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Сколько попыток осталось
    /// </summary>
    public int RemainingAttempts { get; set; }

    /// <summary>
    /// Через сколько секунд можно повторить попытку (если заблокирован)
    /// </summary>
    public int RetryAfterSeconds { get; set; }
}
