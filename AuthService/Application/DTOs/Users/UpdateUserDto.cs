namespace Application.DTOs.Users
{
    /// <summary>
    /// DTO для обновления данных пользователя
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для частичного обновления профиля пользователя.
    /// Все поля являются необязательными - передаются только те данные, которые нужно изменить.
    /// **Особенности:**
    /// - PATCH-совместимый DTO
    /// - Null значения игнорируются при обновлении
    /// - Позволяет точечно изменять данные без влияния на другие поля
    /// </remarks>
    public class UpdateUserDto
    {
        /// <summary>
        /// Новый адрес электронной почты пользователя
        /// </summary>
        /// <example>new.email@example.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Новое имя пользователя
        /// </summary>
        /// <example>Петр</example>
        public string FirstName { get; set; }

        /// <summary>
        /// Новая фамилия пользователя
        /// </summary>
        /// <example>Петров</example>
        public string LastName { get; set; }

        /// <summary>
        /// Новое отчество пользователя
        /// </summary>
        /// <example>Петрович</example>
        public string MiddleName { get; set; }
    }
}
