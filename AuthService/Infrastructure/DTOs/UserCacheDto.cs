using System.Text.Json.Serialization;

namespace Infrastructure.DTOs;

/// <summary>
/// DTO для кеширования пользователя в Tarantool
/// Использует PascalCase свойства с маппингом на snake_case JSON поля
/// </summary>
public class UserCacheDto
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("is_otp_enabled")]
    public bool IsOtpEnabled { get; set; }

    [JsonPropertyName("otp_secret")]
    public string OtpSecret { get; set; }

    [JsonPropertyName("roles_json")]
    public string RolesJson { get; set; } = "[]";
}

/// <summary>
/// DTO для роли в кеше
/// </summary>
public class CachedRoleDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
