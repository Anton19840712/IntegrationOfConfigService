namespace Application.DTOs.OTP
{
    /// <summary>
    /// Модель запроса для подтверждения одноразового пароля (OTP) при двухфакторной аутентификации
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Безопасность:</b> Рекомендуется использовать вместе с временными токенами сессии
    /// для предотвращения атак повторного использования OTP.
    /// </para>
    /// </remarks>
    public class ConfirmOtpRequest
    {
        /// <summary>
        /// Одноразовый пароль (One-Time Password), введенный пользователем для подтверждения операции
        /// </summary>
        /// <value>
        /// Строка, содержащая цифровой код. Обычно состоит из 4-6 цифр, но может содержать буквы
        /// в зависимости от реализации системы аутентификации.
        /// </value>
        /// <example>"123456"</example>
        /// <exception cref="ArgumentNullException">Генерируется при попытке установить null значение</exception>
        /// <exception cref="ArgumentException">Генерируется при передаче пустой строки или кода неверной длины</exception>
        public string Otp { get; set; }
    }
}
