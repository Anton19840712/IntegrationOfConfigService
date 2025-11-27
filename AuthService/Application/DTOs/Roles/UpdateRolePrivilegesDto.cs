namespace Application.DTOs.Roles
{
    /// <summary>
    /// DTO для обновления списка привилегий роли
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для управления привилегиями роли.
    /// Позволяет полностью заменить список привилегий, назначенных роли.
    /// 
    /// **Важно:** Переданный список полностью заменяет текущие привилегии роли.
    /// </remarks>
    public class UpdateRolePrivilegesDto
    {
        /// <summary>
        /// Новый список идентификаторов привилегий для роли
        /// </summary>
        /// <example>["a1b2c3d4-1234-5678-9101-abcdef123456", "d4e5f6a7-4567-8901-2101-def123456789"]</example>
        public List<Guid> PrivilegeIds { get; set; }
    }
}
