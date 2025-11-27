using Application.Interfaces.Service;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// No-op реализация rate limiting для случаев когда Tarantool отключен
/// Всегда разрешает доступ без ограничений
/// </summary>
public class NoOpLoginRateLimiter : ILoginRateLimiter
{
    private readonly ILogger<NoOpLoginRateLimiter> _logger;
    private readonly CacheSettings _cacheSettings;

    public NoOpLoginRateLimiter(
        ILogger<NoOpLoginRateLimiter> logger,
        IOptions<CacheSettings> cacheSettings)
    {
        _logger = logger;
        _cacheSettings = cacheSettings.Value;
    }

    public Task<RateLimitResult> CheckAsync(string loginOrIp)
    {
        _logger.LogDebug("NoOpLoginRateLimiter: Always allowing access for {LoginOrIp}", loginOrIp);

        return Task.FromResult(new RateLimitResult
        {
            Allowed = true,
            RemainingAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
            RetryAfterSeconds = 0
        });
    }

    public Task IncrementAsync(string loginOrIp)
    {
        _logger.LogDebug("NoOpLoginRateLimiter: Ignoring increment for {LoginOrIp}", loginOrIp);
        return Task.CompletedTask;
    }

    public Task ResetAsync(string loginOrIp)
    {
        _logger.LogDebug("NoOpLoginRateLimiter: Ignoring reset for {LoginOrIp}", loginOrIp);
        return Task.CompletedTask;
    }
}
