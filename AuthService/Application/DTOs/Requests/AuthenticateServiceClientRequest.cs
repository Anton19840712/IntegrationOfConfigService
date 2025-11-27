namespace Application.DTOs.Requests
{
    /// <summary>
    /// Запрос аутентификации клиентского сервиса
    /// </summary>
    /// <remarks>
    /// <para>Используется для аутентификации микросервисов и клиентских приложений через client credentials flow.</para>
    /// <para>Предназначен для server-to-server аутентификации без участия пользователя.</para>
    /// <para>Требует предварительной регистрации клиента в системе с выдачей ClientId и ClientSecret.</para>
    /// </remarks>
    public class AuthenticateServiceClientRequest
    {
        /// <summary>
        /// Уникальный идентификатор клиентского приложения/сервиса
        /// </summary>
        /// <remarks>
        /// <para>Выдается при регистрации клиента в системе аутентификации.</para>
        /// <para>Должен соответствовать формату, установленному политикой безопасности.</para>
        /// </remarks>
        /// <example>"web-api-client"</example>
        /// <example>"mobile-app-v2"</example>
        /// <example>"internal-service-scheduler"</example>
        public string ClientId { get; set; }

        /// <summary>
        /// Секретный ключ клиентского приложения/сервиса
        /// </summary>
        /// <remarks>
        /// <para>Конфиденциальная информация, используемая для верификации клиента.</para>
        /// <para>Должен храниться в безопасном месте (secrets manager, environment variables).</para>
        /// <para>Рекомендуется регулярная ротация секретов в соответствии с политикой безопасности.</para>
        /// </remarks>
        /// <example>"5up3r_S3cr3t_K3y_2024!"</example>
        /// <example>"c2VjcmV0LWtleS1mb3ItdGVzdGluZw=="</example>
        public string ClientSecret { get; set; }
    }
}
