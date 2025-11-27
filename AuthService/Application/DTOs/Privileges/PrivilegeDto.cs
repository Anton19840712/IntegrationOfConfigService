namespace Application.DTOs.Privileges
{
    /// <summary>
    /// DTO для отображения информации о привилегии
    /// </summary>
    /// <remarks>
    /// Используется для возврата данных о привилегии в API responses.
    /// Содержит полную информацию о привилегии, включая системные идентификаторы.
    /// </remarks>
    public class PrivilegeDto
    {
        /// <summary>
        /// Уникальный идентификатор привилегии в системе
        /// </summary>
        /// <example>a1b2c3d4-1234-5678-9101-abcdef123456</example>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование привилегии
        /// </summary>
        /// <example>АдминистративныйДоступ</example>
        public string Name { get; set; }
    }
}
