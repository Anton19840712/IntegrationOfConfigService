namespace Application.DTOs
{
    /// <summary>
    /// Представляет стандартизированную модель ответа об ошибке для API.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// HTTP-статус код.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Краткий, машиночитаемый код ошибки.
        /// </summary>
        /// <example>validation_error</example>
        public string Code { get; set; }

        /// <summary>
        /// Человекочитаемое сообщение об ошибке.
        /// </summary>
        /// <example>Произошла одна или несколько ошибок валидации.</example>
        public string Message { get; set; }

        /// <summary>
        /// Коллекция детальных ошибок валидации (ключ - поле, значение - массив ошибок).
        /// Заполняется только для ошибок 400.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; set; }

        /// <summary>
        /// Идентификатор трассировки запроса для корреляции с логами.
        /// </summary>
        public string TraceId { get; set; }
    }
}