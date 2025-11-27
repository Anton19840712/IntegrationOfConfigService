using System.Security.Claims;
using Application.DTOs;
using Application.DTOs.OTP;
using Application.DTOs.Requests;
using Application.Interfaces.Repository;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	/// <summary>
	/// Контроллер аутентификации и управления токенами
	/// </summary>
	/// <remarks>
	/// <para>Предоставляет endpoints для входа в систему, обновления токенов, отзыва токенов и выхода.</para>
	/// <para>Использует JWT-токены с механизмом refresh токенов для безопасной аутентификации.</para>
	/// </remarks>
	/// <remarks>
	/// Инициализирует новый экземпляр контроллера аутентификации
	/// </remarks>
	/// <param name="authService">Сервис аутентификации</param>
	/// <param name="logger">Логгер для записи событий</param>
	/// <param name="refreshTokenRepository"></param>
	[ApiController]
    [Route("api/auth")]
    public class AuthController(Application.Services.AuthService authService, ILogger<Application.Services.AuthService> logger, IRefreshTokenRepository refreshTokenRepository) : ControllerBase
    {
        private readonly Application.Services.AuthService _authService = authService;
        private readonly ILogger<Application.Services.AuthService> _logger = logger;

        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;

		/// <summary>
		/// Аутентификация пользователя в системе
		/// </summary>
		/// <param name="request">Данные для входа</param>
		/// <returns>Access и Refresh токены</returns>
		/// <response code="200">Успешная аутентификация. Возвращает JWT токены</response>
		/// <response code="400">Некорректные данные запроса</response>
		/// <response code="401">Неверные учетные данные</response>
		/// <response code="500">Внутренняя ошибка сервера</response>
		[HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Для тестирования: если передан X-Forwarded-For, используем его
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            try
            {
                var tokens = await _authService.LoginAsync(request.Login, request.Password, ipAddress);

                if (tokens.IsOtpRequired)
                {
                    return Ok(new { tokens.IsOtpRequired, tokens.UserId });
                }

                _logger.LogInformation("Авторизован пользователь - {Login}", request.Login);
                return Ok(new { accessToken = tokens.AccessToken, refreshToken = tokens.RefreshToken });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Неправильный логин или пароль");
            }
        }

        /// <summary>
        /// Второй шаг аутентификации: проверка OTP-кода и завершение процедуры входа в систему
        /// </summary>
        /// <remarks>
        /// <para>
        /// Завершает процесс двухфакторной аутентификации, проверяя одноразовый пароль (OTP),
        /// введенный пользователем. При успешной проверке генерирует и возвращает токены доступа.
        /// </para>
        /// <para>
        /// <b>Безопасность:</b> Включает аудит с IP-адресом клиента. OTP код имеет ограниченное
        /// время жизни (обычно 5-10 минут) и может быть использован только один раз.
        /// </para>
        /// </remarks>
        /// <param name="request">Запрос с данными для верификации OTP</param>
        /// <returns>Токены доступа при успешной аутентификации</returns>
        /// <response code="200">Успешная верификация OTP, возвращены токены доступа</response>
        /// <response code="401">Неверный OTP код или истек срок его действия</response>
        /// <response code="400">Неверный формат запроса или отсутствуют обязательные поля</response>
        /// <response code="500">Внутренняя ошибка сервера при обработке запроса</response>
        [HttpPost("verify-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            // Получаем IP-адрес для аудита безопасности
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("Начало верификации OTP для пользователя {UserId} с IP {IpAddress}",
                request.UserId, ipAddress);

            try
            {
                // Вызываем метод сервиса для проверки OTP и завершения аутентификации
                var response = await _authService.VerifyOtpAndLoginAsync(request.UserId, request.Otp, ipAddress);

                _logger.LogInformation("Успешная верификация OTP для пользователя {UserId}. Аутентификация завершена",
                    request.UserId);

                // В случае успеха возвращаем токены доступа
                return Ok(new
                {
                    response.AccessToken,
                    response.RefreshToken
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Обработка ошибки неверного OTP кода
                _logger.LogWarning("Неудачная попытка верификации OTP для пользователя {UserId}: {ErrorMessage}",
                    request.UserId, ex.Message);

                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // Обработка ошибок валидации входных данных
                _logger.LogWarning("Ошибка валидации при верификации OTP для пользователя {UserId}: {ErrorMessage}",
                    request.UserId, ex.Message);

                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Обработка непредвиденных ошибок сервера
                _logger.LogError(ex, "Непредвиденная ошибка при верификации OTP для пользователя {UserId}",
                    request.UserId);

                return StatusCode(500, new { message = "Произошла внутренняя ошибка сервера при проверке OTP кода." });
            }
        }

        /// <summary>
        /// Обновление Access токена с использованием Refresh токена
        /// </summary>
        /// <remarks>
        /// <para>Позволяет получить новую пару токенов без повторного ввода учетных данных.</para>
        /// <para>Refresh токен должен быть действительным и не отозванным.</para>
        /// </remarks>
        /// <param name="request">Запрос с refresh токеном</param>
        /// <returns>Новая пара Access и Refresh токенов</returns>
        /// <response code="200">Токены успешно обновлены</response>
        /// <response code="401">Недействительный или отозванный refresh токен</response>
        /// <response code="400">Некорректный формат токена</response>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            try
            {
                var (AccessToken, RefreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
                return Ok(new { accessToken = AccessToken, refreshToken = RefreshToken });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Недействительный refresh токен");
            }
        }

        /// <summary>
        /// Отзыв refresh токена
        /// </summary>
        /// <remarks>
        /// <para>Немедленно делает указанный refresh токен недействительным.</para>
        /// <para>Используется для принудительного завершения сеанса или в целях безопасности.</para>
        /// </remarks>
        /// <param name="request">Запрос с токеном для отзыва</param>
        /// <response code="204">Токен успешно отозван</response>
        /// <response code="400">Некорректный формат токена</response>
        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, ipAddress);
            return NoContent();
        }

        /// <summary>
        /// Выход пользователя из системы
        /// </summary>
        /// <remarks>
        /// <para>Выполняет отзыв refresh токена и завершает сеанс пользователя.</para>
        /// <para>Требует действительного access токена в заголовке Authorization.</para>
        /// </remarks>
        /// <param name="request">Запрос с refresh токеном для выхода</param>
        /// <returns>Сообщение об успешном выходе</returns>
        /// <response code="200">Выход выполнен успешно</response>
        /// <response code="401">Неавторизованный доступ</response>
        /// <response code="400">Некорректные данные запроса</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userLogin = User.FindFirst(ClaimTypes.Name)?.Value;

            Guid? parsedUserId = Guid.TryParse(userId, out var guid) ? guid : (Guid?)null;

            await _authService.LogoutAsync(request.RefreshToken, ip, parsedUserId, userLogin);
            return Ok(new { message = "Выход из системы успешен" });
        }
    }
}
