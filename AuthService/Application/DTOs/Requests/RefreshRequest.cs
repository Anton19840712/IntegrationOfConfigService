namespace Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на обновление токена
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Refresh токен, полученный при аутентификации
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string RefreshToken { get; set; }
    }
}
