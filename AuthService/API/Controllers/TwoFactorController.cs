using Application.DTOs;
using Application.DTOs.OTP;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для управления двухфакторной аутентификацией (2FA) пользователя
    /// </summary>
    /// <remarks>
    /// <para>
    /// Предоставляет endpoints для настройки, подтверждения и отключения двухфакторной аутентификации
    /// с использованием одноразовых паролей (OTP) по стандарту TOTP.
    /// </para>
    /// <para>
    /// <b>Требования безопасности:</b> Все операции требуют предварительной аутентификации пользователя.
    /// Для работы с 2FA пользователь должен быть авторизован в системе.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/account/2fa")]
    [Authorize]
    public class TwoFactorController : ControllerBase
    {
        private readonly ILogger<TwoFactorController> _logger;
        private readonly Application.Services.AuthService _authService;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера двухфакторной аутентификации
        /// </summary>
        /// <param name="authService">Сервис аутентификации для операций с 2FA</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public TwoFactorController(Application.Services.AuthService authService, ILogger<TwoFactorController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Инициирует настройку 2FA для текущего пользователя
        /// </summary>
        /// <returns>Секретный ключ для ручного ввода и QR-код в формате base64</returns>
        /// <response code="200">Возвращает данные для настройки 2FA</response>
        /// <response code="400">Ошибка валидации</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpPost("setup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Setup2fa()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("Инициирование настройки двухфакторной аутентификации для пользователя {UserId}", userId);

            try
            {
                var (secretKey, qrCode) = await _authService.GenerateOtpSetupAsync(userId);

                _logger.LogInformation("Успешная генерация данных для настройки 2FA для пользователя {UserId}", userId);

                return Ok(new
                {
                    ManualSetupKey = secretKey,
                    QrCodeImage = $"data:image/png;base64,{Convert.ToBase64String(qrCode)}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации данных для настройки 2FA для пользователя {UserId}", userId);
                return StatusCode(500, new { message = "Произошла внутренняя ошибка сервера при настройке двухфакторной аутентификации." });
            }
        }

        /// <summary>
        /// Подтверждает и активирует 2FA с помощью OTP-кода из приложения аутентификатора
        /// </summary>
        /// <param name="request">Запрос с OTP кодом для подтверждения</param>
        /// <returns>Результат подтверждения 2FA</returns>
        /// <response code="200">2FA успешно активирована</response>
        /// <response code="400">Неверный OTP код или ошибка валидации</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Confirm2fa([FromBody] ConfirmOtpRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("Подтверждение настройки двухфакторной аутентификации для пользователя {UserId}", userId);

            try
            {
                await _authService.ConfirmOtpSetupAsync(userId, request.Otp);

                _logger.LogInformation("Двухфакторная аутентификация успешно активирована для пользователя {UserId}", userId);

                return Ok(new { message = "Двухфакторная аутентификация успешно активирована." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Неудачная попытка подтверждения 2FA для пользователя {UserId}: {ErrorMessage}", userId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при подтверждении двухфакторной аутентификации для пользователя {UserId}", userId);
                return StatusCode(500, new { message = "Произошла внутренняя ошибка сервера при подтверждении двухфакторной аутентификации." });
            }
        }

        /// <summary>
        /// Отключает 2FA для текущего пользователя
        /// </summary>
        /// <returns>Результат отключения 2FA</returns>
        /// <response code="200">2FA успешно отключена</response>
        /// <response code="400">Неверный OTP код или ошибка валидации</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpPost("disable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Disable2fa()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("Отключение двухфакторной аутентификации для пользователя {UserId}", userId);

            try
            {
                await _authService.DisableOtpAsync(userId);

                _logger.LogInformation("Двухфакторная аутентификация успешно отключена для пользователя {UserId}", userId);

                return Ok(new { message = "Двухфакторная аутентификация отключена." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отключении двухфакторной аутентификации для пользователя {UserId}", userId);
                return StatusCode(500, new { message = "Произошла внутренняя ошибка сервера при отключении двухфакторной аутентификации." });
            }
        }
    }
}
