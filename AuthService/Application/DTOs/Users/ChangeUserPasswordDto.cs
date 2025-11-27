namespace Application.DTOs.Users
{
    /// <summary>
    /// DTO для смены пароля пользователя
    /// </summary>
    /// <remarks>
    /// Используется в API endpoints для безопасной смены пароля.
    /// Требует подтверждения текущего пароля для выполнения операции.
    /// **Безопасность:**
    /// - Новый пароль должен соответствовать политике безопасности
    /// - Требуется подтверждение текущего пароля
    /// - Рекомендуется валидация сложности нового пароля
    /// </remarks>
    public class ChangeUserPasswordDto
    {
        /// <summary>
        /// Текущий пароль пользователя для подтверждения
        /// </summary>
        /// <example>СтарыйПароль123</example>
        public string OldPassword { get; set; }

        /// <summary>
        /// Новый пароль пользователя
        /// </summary>
        /// <example>НовыйПароль456</example>
        public string NewPassword { get; set; }
    }
}
