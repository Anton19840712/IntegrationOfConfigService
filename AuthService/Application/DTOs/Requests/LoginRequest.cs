namespace Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на аутентификацию
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Логин пользователя (email или username)
        /// </summary>
        /// <example>user@example.com</example>
        public string Login { get; set; }
        
        /// <summary>
        /// Пароль пользователя
        /// </summary>
        /// <example>string123</example>
        public string Password { get; set; }
    }
}
