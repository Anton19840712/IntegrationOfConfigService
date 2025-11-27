using Application.DTOs;
using Application.DTOs.Users;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;

namespace API.Controllers
{
    /// <summary>
    /// Внутренний контроллер для управления пользователями
    /// </summary>
    /// <remarks>
    /// <para>Предоставляет endpoints для внутренних операций с пользователями системы.</para>
    /// <para>Требует специальных прав доступа (InternalPolicy).</para>
    /// <para>Используется администраторами и внутренними сервисами для управления учетными записями.</para>
    /// </remarks>
    [ApiController]
    [Route("internal/users")]
    [Authorize(Policy = "InternalPolicy")]
    [Produces("application/json")]
    public class InternalUsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<InternalUsersController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр внутреннего контроллера пользователей
        /// </summary>
        /// <param name="userService">Сервис для работы с пользователями</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public InternalUsersController(UserService userService, ILogger<InternalUsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Получение списка всех пользователей системы
        /// </summary>
        /// <remarks>
        /// <para>Возвращает полную информацию обо всех зарегистрированных пользователях.</para>
        /// <para>Включает данные о ролях и привилегиях каждого пользователя.</para>
        /// <para>Требует наличия прав доступа InternalPolicy.</para>
        /// </remarks>
        /// <returns>Список пользователей с детальной информацией</returns>
        /// <response code="200">Успешное получение списка пользователей</response>
        /// <response code="401">Отсутствует авторизация</response>
        /// <response code="403">Недостаточно прав для выполнения операции</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogDebug("Запрос на получение списка всех пользователей системы");

            try
            {
                var users = await _userService.GetAllAsync();
                _logger.LogDebug("Успешно возвращено {UserCount} пользователей", users.Count());
                return Ok(users.Select(u => u.ToDto()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка всех пользователей");
                return StatusCode(500, "Внутренняя ошибка сервера при получении пользователей");
            }
        }
    }
}
