namespace API.Exceptions
{
    /// <summary>
    /// Исключение, выбрасываемое, когда у пользователя недостаточно прав для выполнения действия (403).
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Доступ запрещен. У вас нет необходимых прав для выполнения этого действия.") 
            : base(message) { }
    }
}