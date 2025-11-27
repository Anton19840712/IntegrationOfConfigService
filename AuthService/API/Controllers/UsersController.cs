using System.Security.Claims;
using Application.DTOs.Users;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;

namespace API.Controllers
{
	/// <summary>
	/// Контроллер для управления пользователями системы
	/// </summary>
	/// <remarks>
	/// <para>
	/// Предоставляет полный набор CRUD операций для управления пользователями, включая создание,
	/// редактирование, блокировку/разблокировку и назначение ролей.
	/// </para>
	/// <para>
	/// <b>Требования безопасности:</b> Все операции требуют роли "Admin". Пароли хэшируются
	/// перед сохранением в базу данных. Изменения в учетных записях пользователей логируются
	/// с указанием администратора, выполнившего операцию.
	/// </para>
	/// <example>
	/// Пример workflow управления пользователями:
	/// <code>
	/// 1. GET /api/users - получение списка всех пользователей
	/// 2. POST /api/users - создание нового пользователя
	/// 3. PUT /api/users/{id}/profile - обновление данных пользователя
	/// 4. PUT /api/users/{id}/roles - обновление ролей пользователя
	/// 5. PUT /api/users/{id}/status - блокировка/разблокировка пользователя
	/// </code>
	/// </example>
	/// </remarks>
	/// <remarks>
	/// Инициализирует новый экземпляр контроллера управления пользователями
	/// </remarks>
	/// <param name="userService">Сервис для работы с пользователями</param>
	/// <param name="logger">Логгер для записи событий и ошибок</param>
	[ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController(UserService userService, ILogger<UsersController> logger) : ControllerBase
    {
        private readonly UserService _userService = userService;
        private readonly ILogger<UsersController> _logger = logger;

		/// <summary>
		/// Получает список всех пользователей системы
		/// </summary>
		/// <returns>Коллекция пользователей в формате DTO (без конфиденциальных данных)</returns>
		/// <response code="200">Успешное получение списка пользователей</response>
		/// <response code="401">Пользователь не авторизован</response>
		/// <response code="403">Отсутствуют права доступа</response>
		[HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers()
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Запрос списка всех пользователей администратором {AdminUserId}", currentUserId);

            var users = await _userService.GetAllAsync();
            _logger.LogDebug("Успешно возвращено {UserCount} пользователей", users.Count());

            return Ok(users.Select(u => u.ToDto()));
        }

        /// <summary>
        /// Получает пользователя по уникальному идентификатору
        /// </summary>
        /// <param name="id">GUID идентификатор пользователя</param>
        /// <returns>DTO пользователя</returns>
        /// <response code="200">Пользователь найден</response>
        /// <response code="404">Пользователь с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствуют права доступа</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Запрос пользователя по ID {UserId} администратором {AdminUserId}", id, currentUserId);

            var user = await _userService.GetByIdWithRolesAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден", id);
                return NotFound();
            }

            _logger.LogDebug("Пользователь с ID {UserId} успешно найден", id);
            return Ok(user.ToDto());
        }

        /// <summary>
        /// Создает нового пользователя в системе
        /// </summary>
        /// <param name="dto">DTO с данными для создания пользователя</param>
        /// <returns>Созданный пользователь</returns>
        /// <response code="201">Пользователь успешно создан</response>
        /// <response code="400">Неверные данные запроса</response>
        /// <response code="409">Пользователь с таким логином/email уже существует</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствуют права доступа</response>
        [HttpPost]
        [Authorize(Policy = "AdminOrInternal")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Создание нового пользователя с логином '{UserLogin}' администратором {AdminUserId}", dto.Login, currentUserId);

            try
            {
                var createdUser = await _userService.CreateUserAsync(dto, GetCurrentUserId());
                var userToReturn = await _userService.GetByIdWithRolesAsync(createdUser.Id);
                _logger.LogInformation("Пользователь '{UserLogin}' успешно создан с ID {UserId}", dto.Login, userToReturn.Id);
                return CreatedAtAction(nameof(GetById), new { id = userToReturn.Id }, userToReturn.ToDto());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Ошибка бизнес-логики при создании пользователя: {ErrorMessage}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании пользователя.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        /// <summary>
        /// Обновляет профиль пользователя
        /// </summary>
        /// <param name="id">GUID идентификатор пользователя</param>
        /// <param name="dto">DTO с обновляемыми данными профиля</param>
        /// <response code="204">Профиль успешно обновлен</response>
        /// <response code="404">Пользователь с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствуют права доступа</response>
        [HttpPut("{id:guid}/profile")]
        [Authorize(Policy = "AdminOrInternal")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                await _userService.UpdateUserAsync(id, dto, GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Обновляет роли пользователя
        /// </summary>
        /// <param name="id">GUID идентификатор пользователя</param>
        /// <param name="dto">DTO со списком идентификаторов ролей</param>
        /// <response code="204">Роли пользователя успешно обновлены</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствуют права доступа</response>
        [HttpPut("{id:guid}/roles")]
        [Authorize(Policy = "AdminOrInternal")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesDto dto)
        {
            await _userService.UpdateUserRolesAsync(id, dto.RoleIds, GetCurrentUserId());
            return NoContent();
        }

        /// <summary>
        /// Изменяет пароль пользователя
        /// </summary>
        /// <remarks>
        /// Пользователь может изменять только свой пароль, администратор - любой пароль в системе.
        /// </remarks>
        /// <param name="id">GUID идентификатор пользователя</param>
        /// <param name="dto">DTO с текущим и новым паролем</param>
        /// <response code="204">Пароль успешно изменен</response>
        /// <response code="400">Неверный текущий пароль или новый пароль не соответствует политике</response>
        /// <response code="403">Попытка изменить чужой пароль без прав администратора</response>
        /// <response code="404">Пользователь с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        [HttpPut("{id:guid}/password")]
        [Authorize(Policy = "AdminOrInternal")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangeUserPasswordDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && currentUserId != id)
            {
                _logger.LogWarning("Пользователь {CurrentUserId} попытался изменить пароль пользователя {TargetUserId}", currentUserId, id);
                return Forbid();
            }

            try
            {
                await _userService.ChangePasswordAsync(id, dto, currentUserId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>
        /// Устанавливает статус активности пользователя
        /// </summary>
        /// <param name="id">GUID идентификатор пользователя</param>
        /// <param name="dto">DTO с флагом активности</param>
        /// <response code="204">Статус пользователя успешно обновлен</response>
        /// <response code="404">Пользователь с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствуют права доступа</response>
        [HttpPut("{id:guid}/status")]
        [Authorize(Policy = "AdminOrInternal")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetUserStatusDto dto)
        {
            try
            {
                await _userService.SetUserStatusAsync(id, dto.IsActive, GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Получает идентификатор текущего авторизованного пользователя
        /// </summary>
        /// <returns>GUID пользователя или null если идентификатор не найден</returns>
        private Guid? GetCurrentUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "id");
            return claim != null ? Guid.Parse(claim.Value) : (Guid?)null;
        }
    }
}