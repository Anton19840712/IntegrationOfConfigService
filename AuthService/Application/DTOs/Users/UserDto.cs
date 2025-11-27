using Application.DTOs.Roles;

namespace Application.DTOs.Users
{
    /// <summary>
    /// DTO для отображения полной информации о пользователе
    /// </summary>
    /// <remarks>
    /// Используется в API responses для возврата детализированных данных о пользователе.
    /// Содержит основную информацию, статус, временные метки и полную иерархию прав доступа.
    /// 
    /// **Особенности:**
    /// - `Privileges` содержит объединенный список всех привилегий из всех ролей пользователя
    /// - `CreatedAt` позволяет отслеживать время создания учетной записи
    /// - `IsActive` показывает текущий статус активности пользователя
    /// </remarks>
    public class UserDto
    {
        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        /// <example>f6a7b8c9-6789-0123-4201-f123456789ab</example>
        public Guid Id { get; set; }

        /// <summary>
        /// Логин пользователя для входа в систему
        /// </summary>
        /// <example>ivanovii</example>
        public string Login { get; set; }

        /// <summary>
        /// Адрес электронной почты пользователя
        /// </summary>
        /// <example>ivanov@example.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        /// <example>Иван</example>
        public string FirstName { get; set; }

        /// <summary>
        /// Фамилия пользователя
        /// </summary>
        /// <example>Иванов</example>
        public string LastName { get; set; }

        /// <summary>
        /// Отчество пользователя (необязательно)
        /// </summary>
        /// <example>Иванович</example>
        public string MiddleName { get; set; }

        /// <summary>
        /// Флаг активности пользователя
        /// </summary>
        /// <example>true</example>
        public bool IsActive { get; set; }

        /// <summary>
        /// Дата и время создания учетной записи
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Список ролей пользователя с детализацией привилегий
        /// </summary>
        public List<RoleDto> Roles { get; set; } = [];

        /// <summary>
        /// Объединенный список всех привилегий пользователя (из всех ролей)
        /// </summary>
        /// <example>["УправлениеПользователями", "ДоступКОтчетам"]</example>
        public List<string> Privileges { get; set; } = [];
    }
}
