namespace Application.DTOs.Roles
{
    /// <summary>
    /// DTO для создания новой роли в системе
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для передачи данных при создании роли.
    /// Позволяет создать роль и назначить ей привилегии по их идентификаторам.
    /// </remarks>
    public class CreateRoleDto
    {
        /// <summary>
        /// Наименование роли
        /// </summary>
        /// <example>Администратор</example>
        public string Name { get; set; }

        /// <summary>
        /// Список идентификаторов привилегий, назначаемых роли
        /// </summary>
        /// <example>["a1b2c3d4-1234-5678-9101-abcdef123456", "b2c3d4e5-2345-6789-0102-bcdef1234567"]</example>
        public List<Guid> PrivilegeIds { get; set; } = new List<Guid>();
    }
}
