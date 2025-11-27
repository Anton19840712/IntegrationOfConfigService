namespace API.Exceptions
{
    /// <summary>
    /// Исключение, выбрасываемое, когда пользователь не аутентифицирован (401).
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Отсутствует авторизация. Требуется предоставить действительный токен.") 
            : base(message) { }
    }
}