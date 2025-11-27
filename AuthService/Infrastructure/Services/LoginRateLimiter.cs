using Application.Interfaces.Service;
using Application.Settings;
using SipIntegration.Tarantool.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Infrastructure.Services;

/// <summary>
/// Реализация rate limiting попыток логина через Tarantool
/// </summary>
public class LoginRateLimiter : ILoginRateLimiter
{
    private readonly ITarantoolConnection _tarantool;
    private readonly ILogger<LoginRateLimiter> _logger;
    private readonly CacheSettings _cacheSettings;

    public LoginRateLimiter(
        ITarantoolConnection tarantool,
        ILogger<LoginRateLimiter> logger,
        IOptions<CacheSettings> cacheSettings)
    {
        _tarantool = tarantool;
        _logger = logger;
        _cacheSettings = cacheSettings.Value;
    }

    public async Task<RateLimitResult> CheckAsync(string loginOrIp)
    {
        // Если rate limiting отключен - всегда разрешаем
        if (!_cacheSettings.LoginRateLimitingEnabled)
        {
            return new RateLimitResult
            {
                Allowed = true,
                RemainingAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
                RetryAfterSeconds = 0
            };
        }

        try
        {
            var box = _tarantool.GetClient();
            // Используем JSON string подход для совместимости с ProGaudi.Tarantool.Client
            var response = await box.Call<(string, int, int, int), string>(
                "login_rate_limit_check",
                (loginOrIp, _cacheSettings.LoginRateLimitMaxAttempts, _cacheSettings.LoginRateLimitWindowSeconds, _cacheSettings.LoginRateLimitBlockDurationSeconds)
            );

            var jsonString = response?.Data?.FirstOrDefault();

            if (string.IsNullOrEmpty(jsonString))
            {
                _logger.LogWarning("Tarantool returned null for login_rate_limit_check, allowing by default");
                return new RateLimitResult
                {
                    Allowed = true,
                    RemainingAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
                    RetryAfterSeconds = 0
                };
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<RateLimitCheckDto>(jsonString);
            if (result == null)
            {
                _logger.LogWarning("Failed to deserialize rate limit result, allowing by default");
                return new RateLimitResult
                {
                    Allowed = true,
                    RemainingAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
                    RetryAfterSeconds = 0
                };
            }

            return new RateLimitResult
            {
                Allowed = result.allowed,
                RemainingAttempts = result.remaining,
                RetryAfterSeconds = result.retry_after
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {LoginOrIp}, allowing by default (fail-open)", loginOrIp);
            // Fail-open: при ошибке Tarantool разрешаем вход
            return new RateLimitResult
            {
                Allowed = true,
                RemainingAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
                RetryAfterSeconds = 0
            };
        }
    }

    public async Task IncrementAsync(string loginOrIp)
    {
        if (!_cacheSettings.LoginRateLimitingEnabled)
            return;

        try
        {
            var box = _tarantool.GetClient();
            await box.Call<(string, int), string>("login_rate_limit_increment", (loginOrIp, _cacheSettings.LoginRateLimitWindowSeconds));

            _logger.LogDebug("Incremented login attempt for {LoginOrIp}", loginOrIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing rate limit for {LoginOrIp}", loginOrIp);
            // Не пробрасываем исключение - это не критичная операция
        }
    }

    public async Task ResetAsync(string loginOrIp)
    {
        if (!_cacheSettings.LoginRateLimitingEnabled)
            return;

        try
        {
            var box = _tarantool.GetClient();
            await box.Call<ValueTuple<string>, string>("login_rate_limit_reset", ValueTuple.Create(loginOrIp));

            _logger.LogDebug("Reset login attempts for {LoginOrIp}", loginOrIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for {LoginOrIp}", loginOrIp);
            // Не пробрасываем исключение - это не критичная операция
        }
    }
}

/// <summary>
/// DTO для десериализации ответа от Tarantool login_rate_limit_check
/// </summary>
internal class RateLimitCheckDto
{
    public bool allowed { get; set; }
    public int remaining { get; set; }
    public int retry_after { get; set; }
}
