using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Application.Settings;
using Domain.Entities;
using SipIntegration.Tarantool.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace API.Controllers;

/// <summary>
/// Контроллер для тестирования кеширования и rate limiting через Tarantool
/// </summary>
/// <remarks>
/// Конструктор контроллера
/// </remarks>
[ApiController]
[Route("api/test")]
public class CacheTestController(
	IUserRepository userRepository,
	IUserService userService,
	ILoginRateLimiter rateLimiter,
	ITarantoolConnection? tarantool,
	IRefreshTokenRepository refreshTokenRepository,
	IOptions<CacheSettings> cacheSettings,
	ILogger<CacheTestController> logger,
	IPasswordHasher<User> passwordHasher,
	Infrastructure.Services.TarantoolConnectionManager tarantoolManager) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserService _userService = userService;
    private readonly ILoginRateLimiter _rateLimiter = rateLimiter;
    private readonly ITarantoolConnection? _tarantool = tarantool;
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly CacheSettings _cacheSettings = cacheSettings.Value;
    private readonly ILogger<CacheTestController> _logger = logger;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly Infrastructure.Services.TarantoolConnectionManager _tarantoolManager = tarantoolManager;

	// ==========================================
	// USER CACHE TESTING ENDPOINTS
	// ==========================================

	/// <summary>
	/// Получить пользователя по ID с измерением времени ответа (проверка кеша)
	/// </summary>
	/// <param name="userId">ID пользователя</param>
	/// <returns>Информация о пользователе и время ответа</returns>
	[HttpGet("cache/user/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            sw.Stop();

            if (user == null)
                return NotFound(new { message = "User not found", responseTime = sw.ElapsedMilliseconds });

            // Read cache metadata set by repository (if using CachedUserRepository)
            var cacheSource = HttpContext.Items["CacheSource"] as string;
            var cacheStatus = HttpContext.Items["CacheStatus"] as string;

            // If metadata not set, determine based on configuration
            if (string.IsNullOrEmpty(cacheSource) || string.IsNullOrEmpty(cacheStatus))
            {
                // Using plain UserRepository (no caching decorator)
                bool tarantoolEnabled = _tarantool != null && _tarantool.IsConnected;
                bool userCacheEnabled = _cacheSettings.UserCacheEnabled;

                if (!tarantoolEnabled || !userCacheEnabled)
                {
                    cacheSource = "db-only";
                    cacheStatus = "cache-disabled";
                }
                else
                {
                    // Shouldn't happen, but fallback
                    cacheSource = "unknown";
                    cacheStatus = "unknown";
                }
            }

            return Ok(new
            {
                userId = user.Id,
                login = user.Login,
                email = user.Email,
                isActive = user.IsActive,
                roles = user.UserRoles?.Select(ur => ur.Role?.Name).ToList(),
                responseTime = sw.ElapsedMilliseconds,
                serverProcessingTime = sw.ElapsedMilliseconds,
                source = cacheSource,        // "tarantool", "postgresql", or "db-only"
                cacheStatus = cacheStatus    // "hit", "miss", or "cache-disabled"
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message, responseTime = sw.ElapsedMilliseconds });
        }
    }

    /// <summary>
    /// Обновить email пользователя с инвалидацией кеша
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="request">Новый email</param>
    /// <returns>Результат обновления</returns>
    [HttpPut("cache/user/{userId}/email")]
    public async Task<IActionResult> UpdateUserEmail(Guid userId, [FromBody] UpdateEmailRequest request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // ВАЖНО: Сначала инвалидируем кеш, чтобы GetByIdAsync() загрузил полного пользователя из PostgreSQL
            // Иначе можем получить неполный объект из кеша (без PasswordHash и других полей)
            if (_tarantool != null && _tarantool.IsConnected)
            {
                var box = _tarantool.GetClient();
                await box.Call("user_cache_invalidate", ValueTuple.Create(userId.ToString()));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Email = request.Email;
            _userRepository.Update(user);

            sw.Stop();

            return Ok(new
            {
                message = "Email updated, cache invalidated",
                userId = user.Id,
                newEmail = user.Email,
                responseTime = sw.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message, responseTime = sw.ElapsedMilliseconds });
        }
    }

    /// <summary>
    /// Инвалидировать кеш пользователя в Tarantool
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Результат инвалидации</returns>
    [HttpDelete("cache/user/{userId}")]
    public async Task<IActionResult> InvalidateUserCache(Guid userId)
    {
        try
        {
            if (_tarantool == null || !_tarantool.IsConnected)
            {
                return Ok(new { message = "Tarantool is disabled, no cache to invalidate", userId });
            }

            var box = _tarantool.GetClient();
            var userIdStr = userId.ToString();
            await box.Call("user_cache_invalidate", ValueTuple.Create(userIdStr));

            return Ok(new { message = "User cache invalidated", userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ==========================================
    // RATE LIMITING TESTING ENDPOINTS
    // ==========================================

    /// <summary>
    /// Тестовая попытка входа с проверкой rate limiting
    /// </summary>
    /// <param name="request">Данные для входа</param>
    /// <returns>Результат попытки входа и статус rate limiting</returns>
    [HttpPost("rate-limit/login")]
    public async Task<IActionResult> TestLogin([FromBody] TestLoginRequest request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var rateLimitKey = $"{request.Login}:{request.IpAddress}";

            // 1. Check rate limiting
            var rateLimitResult = await _rateLimiter.CheckAsync(rateLimitKey);

            if (!rateLimitResult.Allowed)
            {
                sw.Stop();
                return StatusCode(429, new
                {
                    message = "Too many login attempts",
                    allowed = false,
                    remainingAttempts = rateLimitResult.RemainingAttempts,
                    retryAfterSeconds = rateLimitResult.RetryAfterSeconds,
                    responseTime = sw.ElapsedMilliseconds
                });
            }

            // 2. Check user credentials in database
            var user = await _userRepository.GetByLoginAsync(request.Login);
            bool passwordCorrect = false;

            if (user != null && user.IsActive)
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                passwordCorrect = verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded;
            }

            if (!passwordCorrect)
            {
                // Increment failed attempts
                await _rateLimiter.IncrementAsync(rateLimitKey);
                sw.Stop();

                return Unauthorized(new
                {
                    message = "Invalid credentials",
                    allowed = true,
                    remainingAttempts = rateLimitResult.RemainingAttempts - 1,
                    responseTime = sw.ElapsedMilliseconds
                });
            }

            // 3. Success - reset counter
            await _rateLimiter.ResetAsync(rateLimitKey);
            sw.Stop();

            return Ok(new
            {
                message = "Login successful",
                userId = user!.Id,
                login = user.Login,
                allowed = true,
                remainingAttempts = 5,
                responseTime = sw.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error testing login for {Login}", request.Login);
            return StatusCode(500, new { error = ex.Message, responseTime = sw.ElapsedMilliseconds });
        }
    }

    /// <summary>
    /// Получить текущий статус rate limiting для логина или IP
    /// </summary>
    /// <param name="loginOrIp">Логин пользователя или IP адрес</param>
    /// <returns>Информация о текущих ограничениях</returns>
    [HttpGet("rate-limit/status/{loginOrIp}")]
    public async Task<IActionResult> GetRateLimitStatus(string loginOrIp)
    {
        try
        {
            var result = await _rateLimiter.CheckAsync(loginOrIp);

            return Ok(new
            {
                loginOrIp,
                allowed = result.Allowed,
                remainingAttempts = result.RemainingAttempts,
                retryAfterSeconds = result.RetryAfterSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit status for {LoginOrIp}", loginOrIp);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Сбросить счетчик неудачных попыток входа
    /// </summary>
    /// <param name="loginOrIp">Логин пользователя или IP адрес</param>
    /// <returns>Результат сброса</returns>
    [HttpDelete("rate-limit/{loginOrIp}")]
    public async Task<IActionResult> ResetRateLimit(string loginOrIp)
    {
        try
        {
            await _rateLimiter.ResetAsync(loginOrIp);

            return Ok(new { message = "Rate limit counter reset", loginOrIp });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for {LoginOrIp}", loginOrIp);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ==========================================
    // TARANTOOL STATS & CONFIG ENDPOINTS
    // ==========================================

    /// <summary>
    /// Получить статистику использования Tarantool
    /// </summary>
    /// <returns>Статистика spaces и feature toggles</returns>
    [HttpGet("tarantool/stats")]
    public async Task<IActionResult> GetTarantoolStats()
    {
        try
        {
            if (_tarantool == null || !_tarantool.IsConnected)
            {
                return Ok(new
                {
                    health = "disabled",
                    spaces = new
                    {
                        userCache = 0,
                        loginAttempts = 0
                    },
                    featureToggles = new
                    {
                        userCacheEnabled = false,
                        userCacheTtl = 0,
                        rateLimitingEnabled = false
                    }
                });
            }

            var box = _tarantool.GetClient();

            // Get spaces info
            var userCacheCount = await box.Call<long>("box.space.user_cache:count");
            var loginAttemptsCount = await box.Call<long>("box.space.login_attempts:count");

            // Get memory info - simplified, just return counts
            return Ok(new
            {
                health = "healthy",
                spaces = new
                {
                    userCache = userCacheCount?.Data?[0] ?? 0,
                    loginAttempts = loginAttemptsCount?.Data?[0] ?? 0
                },
                featureToggles = new
                {
                    userCacheEnabled = _cacheSettings.UserCacheEnabled,
                    userCacheTtl = _cacheSettings.UserCacheTtlSeconds,
                    rateLimitingEnabled = _cacheSettings.LoginRateLimitingEnabled
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Tarantool stats");
            return StatusCode(500, new
            {
                health = "unhealthy",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Очистить все тестовые данные в Tarantool
    /// </summary>
    /// <returns>Результат очистки</returns>
    [HttpPost("tarantool/clear")]
    public async Task<IActionResult> ClearTarantoolData()
    {
        try
        {
            if (_tarantool == null || !_tarantool.IsConnected)
            {
                return Ok(new { message = "Tarantool is disabled, no data to clear" });
            }

            var box = _tarantool.GetClient();

            // Clear all test data
            await box.Call("box.space.user_cache:truncate");
            await box.Call("box.space.login_attempts:truncate");

            return Ok(new { message = "Tarantool test data cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing Tarantool data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить текущую конфигурацию кеширования и rate limiting
    /// </summary>
    /// <returns>Настройки кеша</returns>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        // Check if Tarantool is actually available and enabled
        bool tarantoolAvailable = _tarantoolManager.IsConnected;

        // Features are only truly enabled if Tarantool is available AND settings are enabled
        bool userCacheActuallyEnabled = tarantoolAvailable && _cacheSettings.UserCacheEnabled;
        bool rateLimitActuallyEnabled = tarantoolAvailable && _cacheSettings.LoginRateLimitingEnabled;

        return Ok(new
        {
            cache = new
            {
                userCacheEnabled = userCacheActuallyEnabled,
                userCacheTtlSeconds = _cacheSettings.UserCacheTtlSeconds,
                loginRateLimitingEnabled = rateLimitActuallyEnabled,
                loginRateLimitMaxAttempts = _cacheSettings.LoginRateLimitMaxAttempts,
                loginRateLimitWindowSeconds = _cacheSettings.LoginRateLimitWindowSeconds,
                loginRateLimitBlockDurationSeconds = _cacheSettings.LoginRateLimitBlockDurationSeconds,
                tarantoolAvailable = tarantoolAvailable,
                tarantoolEnabled = _tarantoolManager.IsEnabled
            }
        });
    }

    /// <summary>
    /// Включить Tarantool динамически (без перезапуска)
    /// </summary>
    [HttpPost("tarantool/enable")]
    public IActionResult EnableTarantool()
    {
        try
        {
            _tarantoolManager.Enable();
            return Ok(new
            {
                message = "Tarantool enabled",
                isEnabled = _tarantoolManager.IsEnabled,
                isConnected = _tarantoolManager.IsConnected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling Tarantool");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Отключить Tarantool динамически (без перезапуска)
    /// </summary>
    [HttpPost("tarantool/disable")]
    public IActionResult DisableTarantool()
    {
        try
        {
            _tarantoolManager.Disable();
            return Ok(new
            {
                message = "Tarantool disabled",
                isEnabled = _tarantoolManager.IsEnabled,
                isConnected = _tarantoolManager.IsConnected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling Tarantool");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить статус Tarantool
    /// </summary>
    [HttpGet("tarantool/status")]
    public IActionResult GetTarantoolStatus()
    {
        return Ok(new
        {
            isEnabled = _tarantoolManager.IsEnabled,
            isConnected = _tarantoolManager.IsConnected,
            status = _tarantoolManager.IsEnabled
                ? (_tarantoolManager.IsConnected ? "enabled-connected" : "enabled-disconnected")
                : "disabled"
        });
    }

    // ==========================================
    // DATABASE STATE ENDPOINTS
    // ==========================================

    /// <summary>
    /// Получить всех пользователей из PostgreSQL
    /// </summary>
    /// <returns>Список всех пользователей с токенами</returns>
    [HttpGet("db/users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();

            return Ok(users.Select(u => new
            {
                id = u.Id,
                login = u.Login,
                email = u.Email,
                passwordHash = u.PasswordHash,
                firstName = u.FirstName,
                lastName = u.LastName,
                middleName = u.MiddleName,
                isActive = u.IsActive,
                isOtpEnabled = u.IsOtpEnabled,
                createdAt = u.CreatedAt,
                lastLoginAt = u.LastLoginAt,
                lastLoginIp = u.LastLoginIp,
                roles = u.UserRoles?.Select(ur => ur.Role?.Name).ToList() ?? [],
                refreshTokens = (u.RefreshTokens ?? Enumerable.Empty<RefreshToken>()).Select(t => new
                {
                    token = t.Token,
                    tokenPreview = t.Token.Length > 16 ? t.Token[..16] + "..." : t.Token,
                    expires = t.Expires,
                    isExpired = t.Expires < DateTime.UtcNow,
                    createdAt = t.CreatedAt,
                    createdByIp = t.CreatedByIp,
                    isRevoked = t.RevokedAt.HasValue,
                    revokedAt = t.RevokedAt
                }).ToList()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Создать нового тестового пользователя
    /// </summary>
    /// <param name="request">Данные для создания пользователя</param>
    /// <returns>Созданный пользователь</returns>
    [HttpPost("db/users")]
    public async Task<IActionResult> CreateTestUser([FromBody] CreateTestUserRequest request)
    {
        try
        {
            // Проверяем, не существует ли уже пользователь с таким логином
            var existingUser = await _userRepository.GetByLoginAsync(request.Login);
            if (existingUser != null)
            {
                return Conflict(new { error = $"User with login '{request.Login}' already exists" });
            }

            // Создаем DTO для UserService
            var createUserDto = new Application.DTOs.Users.CreateUserDto
            {
                Login = request.Login,
                Email = request.Email ?? $"{request.Login}@test.com",
                Password = request.Password,
                FirstName = request.FirstName ?? "Test",
                LastName = request.LastName ?? "User",
                MiddleName = "",
                RoleIds = [] // Пустой список ролей для тестового пользователя
            };

            // Используем UserService, который публикует событие UserCreatedEvent в RabbitMQ
            var user = await _userService.CreateUserAsync(createUserDto, createdByUserId: null);
            _logger.LogInformation("Test user {Login} created with ID {UserId} and UserCreatedEvent published", user.Login, user.Id);

            return Ok(new
            {
                id = user.Id,
                login = user.Login,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                isActive = user.IsActive,
                createdAt = user.CreatedAt,
                message = "User created and UserCreatedEvent published to RabbitMQ"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test user");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Удалить пользователя из базы данных PostgreSQL
    /// </summary>
    /// <param name="userId">ID пользователя для удаления</param>
    /// <returns>Результат удаления</returns>
    [HttpDelete("db/users/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var login = user.Login;
            _userRepository.Delete(user);
            _logger.LogInformation("User {UserId} ({Login}) deleted", userId, login);

            return Ok(new { message = $"User {login} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить все refresh токены пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список токенов с информацией о сроке действия</returns>
    [HttpGet("db/user/{userId}/tokens")]
    public async Task<IActionResult> GetUserTokens(Guid userId)
    {
        try
        {
            var tokens = await _refreshTokenRepository.GetByUserIdAsync(userId);
            var now = DateTime.UtcNow;

            var tokenInfo = tokens.Select(t => new
            {
                token = string.Concat(t.Token.AsSpan(0, 8), "..."),
                createdAt = t.CreatedAt,
                expiresAt = t.Expires,
                isExpired = t.Expires < now,
                isRevoked = t.RevokedAt.HasValue,
                revokedAt = t.RevokedAt,
                ttlSeconds = t.Expires > now ? (long)(t.Expires - now).TotalSeconds : 0
            }).ToList();

            return Ok(new
            {
                userId,
                tokens = tokenInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tokens");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить всех закешированных пользователей из Tarantool
    /// </summary>
    /// <returns>Список закешированных пользователей с TTL</returns>
    [HttpGet("tarantool/user-cache/all")]
    public async Task<IActionResult> GetAllCachedUsers()
    {
        try
        {
            if (_tarantool == null || !_tarantool.IsConnected)
            {
                return Ok(new
                {
                    message = "Tarantool is disabled",
                    count = 0,
                    data = Array.Empty<object>()
                });
            }

            var box = _tarantool.GetClient();

            // Call Lua function - returns JSON string
            var response = await box.Call<string>("user_cache_get_all");

            if (response?.Data == null || response.Data.Length == 0)
            {
                return Ok(new
                {
                    message = "Cached users",
                    count = 0,
                    data = Array.Empty<CachedUserInfoDto>()
                });
            }

            // Parse JSON string
            var jsonString = response.Data[0];
            var users = System.Text.Json.JsonSerializer.Deserialize<List<CachedUserInfoDto>>(jsonString);

            return Ok(new
            {
                message = "Cached users",
                count = users?.Count ?? 0,
                data = users ?? []
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить все записи о попытках входа из Tarantool
    /// </summary>
    /// <returns>Список попыток входа с блокировками</returns>
    [HttpGet("tarantool/login-attempts/all")]
    public async Task<IActionResult> GetAllLoginAttempts()
    {
        try
        {
            if (_tarantool == null || !_tarantool.IsConnected)
            {
                return Ok(new
                {
                    message = "Tarantool is disabled",
                    count = 0,
                    data = Array.Empty<object>()
                });
            }

            var box = _tarantool.GetClient();

            // Call Lua function - returns JSON string
            var response = await box.Call<string>("login_attempts_get_all");

            if (response?.Data == null || response.Data.Length == 0)
            {
                return Ok(new
                {
                    message = "Login attempts",
                    count = 0,
                    data = Array.Empty<LoginAttemptInfoDto>()
                });
            }

            // Parse JSON string
            var jsonString = response.Data[0];
            var attempts = System.Text.Json.JsonSerializer.Deserialize<List<LoginAttemptInfoDto>>(jsonString);

            return Ok(new
            {
                message = "Login attempts",
                count = attempts?.Count ?? 0,
                data = attempts ?? []
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting login attempts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ==========================================
    // FEATURE TOGGLES
    // ==========================================

    /// <summary>
    /// Включить/выключить кеширование пользователей
    /// </summary>
    /// <param name="request">Флаг включения</param>
    /// <returns>Новый статус кеширования</returns>
    [HttpPost("toggle-user-cache")]
    public IActionResult ToggleUserCache([FromBody] ToggleRequest request)
    {
        _cacheSettings.UserCacheEnabled = request.Enabled;

        _logger.LogWarning("User Cache {Status} by admin request",
            request.Enabled ? "ENABLED" : "DISABLED");

        return Ok(new
        {
            userCacheEnabled = _cacheSettings.UserCacheEnabled,
            message = $"User Cache is now {(request.Enabled ? "enabled ✅" : "disabled ❌")}"
        });
    }

    /// <summary>
    /// Включить/выключить rate limiting для попыток входа
    /// </summary>
    /// <param name="request">Флаг включения</param>
    /// <returns>Новый статус rate limiting</returns>
    [HttpPost("toggle-rate-limiting")]
    public IActionResult ToggleRateLimiting([FromBody] ToggleRequest request)
    {
        _cacheSettings.LoginRateLimitingEnabled = request.Enabled;

        _logger.LogWarning("Rate Limiting {Status} by admin request",
            request.Enabled ? "ENABLED" : "DISABLED");

        return Ok(new
        {
            rateLimitingEnabled = _cacheSettings.LoginRateLimitingEnabled,
            message = $"Rate Limiting is now {(request.Enabled ? "enabled ✅" : "disabled ❌")}"
        });
    }
}

public class ToggleRequest
{
    public bool Enabled { get; set; }
}

public class UpdateEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class TestLoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class CachedUserInfoDto
{
    public string user_id { get; set; } = string.Empty;
    public string login { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool is_active { get; set; }
    public string roles_json { get; set; } = string.Empty;
    public ulong expires_at { get; set; }
    public long ttl_remaining { get; set; }
}

public class CreateTestUserRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class LoginAttemptInfoDto
{
    public string login_or_ip { get; set; } = string.Empty;
    public int attempt_count { get; set; }
    public ulong first_attempt { get; set; }
    public ulong expires_at { get; set; }
    public ulong blocked_until { get; set; }
    public long ttl_remaining { get; set; }
    public bool is_blocked { get; set; }
    public long block_ttl_remaining { get; set; }
}
