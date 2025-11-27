namespace Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }                        // Идентификатор
        public string Login { get; set; }                   // Логин (уникальный)
        public string PasswordHash { get; set; }            // Хэш пароля
        public string FirstName { get; set; }               // Имя
        public string LastName { get; set; }                // Фамилия
        public string MiddleName { get; set; }              // Отчество
        public string Email { get; set; }                   // Email
        public bool IsActive { get; set; } = true;          // Активен/Заблокирован
        public DateTime CreatedAt { get; set; }             // Дата создания
        public DateTime? LastLoginAt { get; set; }          // Когда был последний вход
        public string? LastLoginIp { get; set; }             // С какого IP был вход

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Поддержка OTP
        public bool IsOtpEnabled { get; set; } = false;
        public string? OtpSecretKey { get; set; }
        public DateTime? OtpEnabledAt { get; set; }
    }
}
