using Application.DTOs.Privileges;

namespace Application.DTOs.Roles
{
    /// <summary>
    /// DTO для отображения информации о роли с детализацией привилегий
    /// </summary>
    /// <remarks>
    /// Используется для возврата полных данных о роли в API responses.
    /// Содержит информацию о роли и полные данные о назначенных привилегиях.
    /// </remarks>
    public class RoleDto
    {
        /// <summary>
        /// Уникальный идентификатор роли в системе
        /// </summary>
        /// <example>c3d4e5f6-3456-7890-1101-cdef12345678</example>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование роли
        /// </summary>
        /// <example>Администратор</example>
        public string Name { get; set; }

        /// <summary>
        /// Список привилегий, назначенных роли
        /// </summary>
        public List<PrivilegeDto> Privileges { get; set; } = new List<PrivilegeDto>();
    }
}
