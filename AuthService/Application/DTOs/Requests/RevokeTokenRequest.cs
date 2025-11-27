namespace Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на отзыв токена
    /// </summary>
    public class RevokeTokenRequest
    {
        /// <summary>
        /// Refresh токен для отзыва
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string RefreshToken { get; set; }
    }
}
