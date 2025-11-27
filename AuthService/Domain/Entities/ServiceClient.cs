namespace Domain.Entities
{
    public class ServiceClient
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; }       // Уникальный идентификатор сервиса
        public string ClientSecretHash { get; set; } // Хэш секретного ключа
        public string Name { get; set; }           // Название сервиса для понятности
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}
