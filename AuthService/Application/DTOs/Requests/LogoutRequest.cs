namespace Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на выход из системы
    /// </summary>
    public class LogoutRequest
    {
        /// <summary>
        /// Refresh токен для инвалидации
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string RefreshToken { get; set; }
    }
}

