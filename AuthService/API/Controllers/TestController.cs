using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для тестирования интеграции с внешними сервисами
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        /// <summary>
        /// Конструктор контроллера
        /// </summary>
        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Тестовый endpoint для проверки отправки исключений в Sentry
        /// </summary>
        /// <returns>Статус выполнения теста</returns>
        [HttpGet("test-sentry")]
        public IActionResult TestSentry()
        {
            try
            {
                throw new InvalidOperationException("Это тестовое исключение для проверки Sentry.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Произошла тестовая ошибка Sentry.");
                // Sentry SDK автоматически перехватит это событие благодаря интеграции с ILogger
                // Или можно отправить вручную: SentrySdk.CaptureException(e);
                return StatusCode(500, "Ошибка была отправлена в Sentry");
            }
        }
    }
}
