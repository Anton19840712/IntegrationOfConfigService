namespace Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }     // Может быть null для системных событий
        public string UserLogin { get; set; } // Логин пользователя или Сервис
        public string Action { get; set; }    // Например: "LOGIN_SUCCESS", "TOKEN_REVOKE"
        public string Description { get; set; } // Человеко-понятное описание на русском
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string? AdditionalData { get; set; } // JSON/строка с деталями
    }
}
