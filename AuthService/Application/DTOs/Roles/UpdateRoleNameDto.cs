namespace Application.DTOs.Roles
{
    /// <summary>
    /// DTO для обновления наименования роли
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для частичного обновления роли (PATCH операции).
    /// Позволяет изменить только наименование роли без затрагивания привилегий.
    /// </remarks>
    public class UpdateRoleNameDto
    {
        /// <summary>
        /// Новое наименование роли
        /// </summary>
        /// <example>СуперАдминистратор</example>
        public string Name { get; set; }
    }
}
