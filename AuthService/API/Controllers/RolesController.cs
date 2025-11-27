using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;
using Application.DTOs.Roles;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для управления ролями и их привилегиями в системе
    /// </summary>
    /// <remarks>
    /// <para>
    /// Предоставляет CRUD операции для управления ролями пользователей и назначения привилегий.
    /// Роли используются для группировки пользователей и централизованного управления правами доступа.
    /// Доступ разрешен только пользователям с ролью "Admin".
    /// </para>
    /// <para>
    /// <b>Требования безопасности:</b> Все операции требуют роли "Admin". 
    /// Изменения в ролях и привилегиях логируются с указанием пользователя, выполнившего операцию.
    /// </para>
    /// <example>
    /// Пример workflow управления ролями:
    /// <code>
    /// 1. GET /api/roles - получение списка всех ролей
    /// 2. POST /api/roles - создание новой роли
    /// 3. PUT /api/roles/{id}/name - обновление названия роли
    /// 4. PUT /api/roles/{id}/privileges - обновление списка привилегий
    /// 5. DELETE /api/roles/{id} - удаление роли
    /// </code>
    /// </example>
    /// </remarks>
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly RoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера управления ролями
        /// </summary>
        /// <param name="roleService">Сервис для работы с ролями и привилегиями</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public RolesController(RoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Получает список всех ролей системы с их привилегиями
        /// </summary>
        /// <returns>Коллекция ролей с детальной информацией о привилегиях</returns>
        /// <response code="200">Успешное получение списка ролей</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Запрос на получение списка всех ролей с привилегиями пользователем {UserId}", currentUserId);

            var roles = await _roleService.GetAllRolesWithPrivilegesAsync();
            var result = roles.Select(r => r.ToDto()).ToList();

            _logger.LogDebug("Успешно возвращено {RoleCount} ролей с привилегиями", result.Count);

            return Ok(result);
        }

        /// <summary>
        /// Получает роль по уникальному идентификатору с полной информацией о привилегиях
        /// </summary>
        /// <param name="id">GUID идентификатор роли</param>
        /// <returns>DTO роли с детальной информацией о привилегиях</returns>
        /// <response code="200">Роль найдена</response>
        /// <response code="404">Роль с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Запрос роли по ID {RoleId} пользователем {UserId}", id, currentUserId);

            var role = await _roleService.GetRoleByIdWithPrivilegesAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Роль с ID {RoleId} не найдена", id);
                return NotFound();
            }

            _logger.LogDebug("Роль с ID {RoleId} успешно найдена, содержит {PrivilegeCount} привилегий", id, role.RolePrivileges.Count);

            return Ok(role.ToDto());
        }

        /// <summary>
        /// Создает новую роль в системе
        /// </summary>
        /// <param name="dto">DTO с данными для создания роли</param>
        /// <returns>Созданная роль</returns>
        /// <response code="201">Роль успешно создана</response>
        /// <response code="400">Неверные данные запроса</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Создание новой роли с именем '{RoleName}' пользователем {UserId}", dto.Name, currentUserId);

            var role = await _roleService.CreateRoleAsync(dto.Name, dto.PrivilegeIds, GetCurrentUserId());

            _logger.LogInformation("Роль '{RoleName}' успешно создана с ID {RoleId}", dto.Name, role.Id);

            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role.ToDto());
        }

        /// <summary>
        /// Обновляет наименование роли
        /// </summary>
        /// <param name="id">GUID идентификатор роли</param>
        /// <param name="dto">DTO с новым наименованием роли</param>
        /// <response code="204">Наименование роли успешно обновлено</response>
        /// <response code="404">Роль с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPut("{id:guid}/name")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateName(Guid id, [FromBody] UpdateRoleNameDto dto)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound();

            await _roleService.UpdateRoleNameAsync(role, dto.Name, GetCurrentUserId());
            return NoContent();
        }

        /// <summary>
        /// Обновляет список привилегий роли
        /// </summary>
        /// <param name="id">GUID идентификатор роли</param>
        /// <param name="dto">DTO со списком идентификаторов привилегий</param>
        /// <response code="204">Список привилегий роли успешно обновлен</response>
        /// <response code="404">Роль с указанным ID не найдена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPut("{id:guid}/privileges")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePrivileges(Guid id, [FromBody] UpdateRolePrivilegesDto dto)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound();

            await _roleService.UpdateRolePrivilegesAsync(id, dto.PrivilegeIds, GetCurrentUserId());
            return NoContent();
        }

        /// <summary>
        /// Назначает привилегию указанной роли
        /// </summary>
        /// <param name="roleId">GUID идентификатор роли</param>
        /// <param name="privilegeId">GUID идентификатор привилегии</param>
        /// <response code="204">Привилегия успешно назначена роли</response>
        /// <response code="404">Роль или привилегия не найдены</response>
        /// <response code="400">Привилегия уже назначена роли</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost("{roleId:guid}/privileges/{privilegeId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AssignPrivilege(Guid roleId, Guid privilegeId)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Назначение привилегии {PrivilegeId} роли {RoleId} пользователем {UserId}", privilegeId, roleId, currentUserId);

            await _roleService.AssignPrivilegeAsync(roleId, privilegeId, currentUserId);

            _logger.LogInformation("Привилегия {PrivilegeId} успешно назначена роли {RoleId}", privilegeId, roleId);

            return NoContent();
        }

        /// <summary>
        /// Удаляет привилегию у указанной роли
        /// </summary>
        /// <param name="roleId">GUID идентификатор роли</param>
        /// <param name="privilegeId">GUID идентификатор привилегии</param>
        /// <response code="204">Привилегия успешно удалена у роли</response>
        /// <response code="404">Роль или привилегия не найдены</response>
        /// <response code="400">Привилегия не назначена роли</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpDelete("{roleId:guid}/privileges/{privilegeId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemovePrivilege(Guid roleId, Guid privilegeId)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogDebug("Удаление привилегии {PrivilegeId} у роли {RoleId} пользователем {UserId}", privilegeId, roleId, currentUserId);

            await _roleService.RemovePrivilegeAsync(roleId, privilegeId, currentUserId);

            _logger.LogInformation("Привилегия {PrivilegeId} успешно удалена у роли {RoleId}", privilegeId, roleId);

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