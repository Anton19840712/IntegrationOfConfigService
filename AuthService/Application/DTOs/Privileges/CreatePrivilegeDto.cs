namespace Application.DTOs.Privileges
{
    /// <summary>
    /// DTO для создания новой привилегии в системе
    /// </summary>
    public class CreatePrivilegeDto
    {
        /// <summary>
        /// Уникальное наименование привилегии
        /// </summary>
        /// <example>АдминистративныйДоступ</example>
        public string Name { get; set; }
    }
}
