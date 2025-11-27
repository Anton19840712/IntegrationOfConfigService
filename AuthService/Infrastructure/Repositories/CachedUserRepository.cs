using Application.Interfaces.Repository;
using Application.Settings;
using Infrastructure.DTOs;
using Infrastructure.Services;
using Domain.Entities;
using SipIntegration.Tarantool.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Repositories;

/// <summary>
/// Декоратор для UserRepository с кешированием в Tarantool
/// Реализует паттерн Cache-Aside (Lazy Loading) с активной инвалидацией
/// Поддерживает динамическое включение/отключение через TarantoolConnectionManager
/// </summary>
public class CachedUserRepository(
	IUserRepository inner,
	ITarantoolConnection cache,
	ILogger<CachedUserRepository> logger,
	IOptions<CacheSettings> cacheSettings,
	IHttpContextAccessor httpContextAccessor,
	TarantoolConnectionManager? tarantoolManager = null) : IUserRepository
{
    private readonly IUserRepository _inner = inner;
    private readonly ITarantoolConnection _cache = cache;
    private readonly ILogger<CachedUserRepository> _logger = logger;
    private readonly CacheSettings _cacheSettings = cacheSettings.Value;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly TarantoolConnectionManager? _tarantoolManager = tarantoolManager;

	// ==========================================
	// CACHED METHODS (Cache-Aside pattern)
	// ==========================================

	public async Task<User> GetByIdAsync(Guid id)
    {
        // Check if caching is enabled (both in settings and via manager)
        bool cachingEnabled = _cacheSettings.UserCacheEnabled &&
                              (_tarantoolManager == null || _tarantoolManager.IsEnabled);

        // Если кеш отключен - используем только БД
        if (!cachingEnabled)
        {
            SetCacheMetadata("db-only", "cache-disabled");
            return await _inner.GetByIdAsync(id);
        }

        var userId = id.ToString();

        try
        {
            // 1. Проверяем кеш
            var box = _cache.GetClient();
            var response = await box.Call<ValueTuple<string>, string>("user_cache_get", ValueTuple.Create(userId));
            var jsonString = response?.Data?.FirstOrDefault();

            if (!string.IsNullOrEmpty(jsonString) && jsonString != "null")
            {
                var cached = System.Text.Json.JsonSerializer.Deserialize<UserCacheDto>(jsonString);
                if (cached != null)
                {
                    _logger.LogDebug("Cache HIT for user {UserId}", userId);
                    SetCacheMetadata("tarantool", "hit");
                    return MapFromCache(cached);
                }
            }

            _logger.LogDebug("Cache MISS for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for user {UserId}, fallback to DB", userId);
        }

        // 2. Cache miss - идем в PostgreSQL
        SetCacheMetadata("postgresql", "miss");
        var user = await _inner.GetByIdAsync(id);
        if (user == null)
            return null;

        // 3. Сохраняем в кеш (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await SetCacheAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for user {UserId}", userId);
            }
        });

        return user;
    }

    public async Task<User> GetByLoginAsync(string login)
    {
        // Для GetByLogin кеш не используем - поиск по логину редкий (только при логине)
        // Можно добавить отдельный индекс в Tarantool, но пока оставим простую реализацию
        return await _inner.GetByLoginAsync(login);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        // Для GetByEmail кеш не используем - поиск по email редкий
        return await _inner.GetByEmailAsync(email);
    }

    // ==========================================
    // WRITE METHODS (with cache invalidation)
    // ==========================================

    public async Task AddAsync(User user)
    {
        await _inner.AddAsync(user);

        // При создании нового пользователя кеш не создаем
        // Он будет создан при первом чтении
    }

    public void Update(User user)
    {
        _inner.Update(user);

        // Инвалидируем кеш (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await InvalidateCacheAsync(user.Id.ToString());
                _logger.LogDebug("Cache invalidated for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for user {UserId}", user.Id);
            }
        });
    }

    public void Delete(User user)
    {
        _inner.Delete(user);

        // Инвалидируем кеш (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await InvalidateCacheAsync(user.Id.ToString());
                _logger.LogDebug("Cache invalidated for deleted user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for deleted user {UserId}", user.Id);
            }
        });
    }

    // ==========================================
    // NON-CACHED METHODS (passthrough)
    // ==========================================

    public Task<User> GetByIdWithPreviegesAsync(Guid id)
    {
        // Этот метод используется редко, кешировать не нужно
        return _inner.GetByIdWithPreviegesAsync(id);
    }

    public Task<IReadOnlyList<User>> GetAllAsync()
    {
        // GetAll не кешируем - используется редко (админка)
        return _inner.GetAllAsync();
    }

    public Task<bool> AnyWithLoginOrEmailAsync(string login, string email)
    {
        // Проверка уникальности - не кешируем
        return _inner.AnyWithLoginOrEmailAsync(login, email);
    }

    // ==========================================
    // HELPER METHODS
    // ==========================================

    private async Task SetCacheAsync(User user)
    {
        var rolesJson = SerializeRoles(user);
        var box = _cache.GetClient();

        await box.Call(
            "user_cache_set",
            (user.Id.ToString(), user.Login, user.Email ?? string.Empty, user.IsActive, rolesJson, _cacheSettings.UserCacheTtlSeconds)
        );
    }

    private async Task InvalidateCacheAsync(string userId)
    {
        var box = _cache.GetClient();
        await box.Call("user_cache_invalidate", ValueTuple.Create(userId));
    }

    private User MapFromCache(UserCacheDto cached)
    {
        var user = new User
        {
            Id = Guid.Parse(cached.UserId),
            Login = cached.Login,
            Email = cached.Email,
            IsActive = cached.IsActive,
            UserRoles = []
        };

        // Десериализуем роли
        if (!string.IsNullOrEmpty(cached.RolesJson))
        {
            try
            {
                var roles = JsonSerializer.Deserialize<List<CachedRoleDto>>(cached.RolesJson);
                if (roles != null)
                {
                    foreach (var roleDto in roles)
                    {
                        var role = new Role
                        {
                            Id = Guid.Parse(roleDto.RoleId),
                            Name = roleDto.RoleName
                        };

                        user.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id,
                            Role = role
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deserializing roles from cache for user {UserId}", cached.UserId);
            }
        }

        return user;
    }

    private static string SerializeRoles(User user)
    {
        if (user.UserRoles == null || user.UserRoles.Count == 0)
            return "[]";

        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => new CachedRoleDto
            {
                RoleId = ur.Role.Id.ToString(),
                RoleName = ur.Role.Name
            })
            .ToList();

        return JsonSerializer.Serialize(roles);
    }

    private class CachedRoleDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Записывает метаданные о источнике данных в HttpContext для использования в контроллере
    /// </summary>
    private void SetCacheMetadata(string source, string status)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["CacheSource"] = source;
            httpContext.Items["CacheStatus"] = status;
        }
    }
}
