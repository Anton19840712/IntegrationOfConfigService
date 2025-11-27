using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;
using Application.DTOs.Privileges;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для управления привилегиями (правами доступа) в системе
    /// </summary>
    /// <remarks>
    /// <para>
    /// Предоставляет CRUD операции для управления привилегиями системы. 
    /// Доступ разрешен только пользователям с ролью "Admin".
    /// Привилегии используются для разграничения прав доступа к функционалу системы.
    /// </para>
    /// <para>
    /// <b>Требования безопасности:</b> Все операции требуют роли "Admin". 
    /// Изменения привилегий логируются с указанием пользователя, выполнившего операцию.
    /// </para>
    /// <example>
    /// Пример workflow управления привилегиями:
    /// <code>
    /// 1. GET /api/privileges - получение списка всех привилегий
    /// 2. POST /api/privileges - создание новой привилегии
    /// 3. PUT /api/privileges/{id} - обновление привилегии
    /// 4. DELETE /api/privileges/{id} - удаление привилегии
    /// </code>
    /// </example>
    /// </remarks>
    [ApiController]
    [Route("api/privileges")]
    [Authorize(Roles = "Admin")]
    public class PrivilegesController : ControllerBase
    {
        private readonly PrivilegeService _privilegeService;
        private readonly ILogger<PrivilegesController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера управления привилегиями
        /// </summary>
        /// <param name="privilegeService">Сервис для работы с привилегиями</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public PrivilegesController(PrivilegeService privilegeService, ILogger<PrivilegesController> logger)
        {
            _privilegeService = privilegeService;
            _logger = logger;
        }

        /// <summary>
        /// Получает список всех привилегий системы
        /// </summary>
        /// <returns>Коллекция привилегий в формате DTO</returns>
        /// <response code="200">Успешное получение списка привилегий</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PrivilegeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogDebug("Запрос на получение списка всех привилегий пользователем {UserId}", GetCurrentUserId());

            var privileges = await _privilegeService.GetAllPrivilegesAsync();
            var privilegeDtos = privileges.Select(p => p.ToDto());

            _logger.LogDebug("Успешно возвращено {Count} привилегий", privileges.Count());

            return Ok(privilegeDtos);
        }

        /// <summary>
        /// Получает привилегию по уникальному идентификатору
        /// </summary>
        /// <param name="id">GUID идентификатор привилегии</param>
        /// <returns>DTO привилегии</returns>
        /// <response code="200">Привилегия найдена</response>
        /// <response code="404">Привилегия с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PrivilegeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogDebug("Запрос привилегии по ID {PrivilegeId} пользователем {UserId}", id, GetCurrentUserId());

            var privilege = await _privilegeService.GetPrivilegeByIdAsync(id);
            if (privilege == null)
            {
                _logger.LogWarning("Привилегия с ID {PrivilegeId} не найдена", id);
                return NotFound();
            }

            _logger.LogDebug("Привилегия с ID {PrivilegeId} успешно найдена", id);

            return Ok(privilege.ToDto());
        }

        /// <summary>
        /// Создает новую привилегию в системе
        /// </summary>
        /// <param name="dto">DTO с данными для создания привилегии</param>
        /// <returns>Созданная привилегия</returns>
        /// <response code="201">Привилегия успешно создана</response>
        /// <response code="400">Неверные данные запроса</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost]
        [ProducesResponseType(typeof(PrivilegeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreatePrivilegeDto dto)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Создание новой привилегии с именем '{PrivilegeName}' пользователем {UserId}", dto.Name, currentUserId);

            var privilege = await _privilegeService.CreatePrivilegeAsync(dto.Name, currentUserId);

            _logger.LogInformation("Привилегия '{PrivilegeName}' успешно создана с ID {PrivilegeId}", dto.Name, privilege.Id);

            return CreatedAtAction(nameof(GetById), new { id = privilege.Id }, privilege.ToDto());
        }

        /// <summary>
        /// Обновляет существующую привилегию
        /// </summary>
        /// <param name="id">GUID идентификатор обновляемой привилегии</param>
        /// <param name="dto">DTO с новыми данными привилегии</param>
        /// <response code="204">Привилегия успешно обновлена</response>
        /// <response code="404">Привилегия с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreatePrivilegeDto dto)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Обновление привилегии с ID {PrivilegeId} пользователем {UserId}", id, currentUserId);

            var privilege = await _privilegeService.GetPrivilegeByIdAsync(id);
            if (privilege == null)
            {
                _logger.LogWarning("Привилегия с ID {PrivilegeId} не найдена при попытке обновления", id);
                return NotFound();
            }

            _logger.LogInformation("Изменение имени привилегии с '{OldName}' на '{NewName}'", privilege.Name, dto.Name);

            await _privilegeService.UpdatePrivilegeAsync(privilege, dto.Name, currentUserId);

            _logger.LogDebug("Привилегия с ID {PrivilegeId} успешно обновлена", id);

            return NoContent();
        }

        /// <summary>
        /// Удаляет привилегию из системы
        /// </summary>
        /// <param name="id">GUID идентификатор удаляемой привилегии</param>
        /// <response code="204">Привилегия успешно удалена</response>
        /// <response code="404">Привилегия с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Удаление привилегии с ID {PrivilegeId} пользователем {UserId}", id, currentUserId);

            var privilege = await _privilegeService.GetPrivilegeByIdAsync(id);
            if (privilege == null)
            {
                _logger.LogWarning("Привилегия с ID {PrivilegeId} не найдена при попытке удаления", id);
                return NotFound();
            }

            await _privilegeService.DeletePrivilegeAsync(privilege, currentUserId);

            _logger.LogInformation("Привилегия '{PrivilegeName}' с ID {PrivilegeId} успешно удалена", privilege.Name, id);

            return NoContent();
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