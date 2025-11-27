namespace Application.DTOs.Users
{
    /// <summary>
    /// DTO для создания нового пользователя в системе
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для регистрации или создания пользователей администратором.
    /// Содержит все необходимые данные для создания учетной записи пользователя.
    /// **Примечания:**
    /// - Логин должен быть уникальным в системе
    /// - Email должен быть валидным и уникальным
    /// - Пароль должен соответствовать политике безопасности
    /// - Отчество является необязательным полем
    /// </remarks>
    public class CreateUserDto
    {
        /// <summary>
        /// Уникальный логин для входа в систему
        /// </summary>
        /// <example>ivanovii</example>
        public string Login { get; set; }

        /// <summary>
        /// Адрес электронной почты пользователя
        /// </summary>
        /// <example>ivanov@example.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        /// <example>SecurePass123!</example>
        public string Password { get; set; }

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
        /// Список идентификаторов ролей, назначаемых пользователю
        /// </summary>
        /// <example>["c3d4e5f6-3456-7890-1101-cdef12345678", "d4e5f6a7-4567-8901-2101-def123456789"]</example>
        public List<Guid> RoleIds { get; set; } = [];
    }
}
