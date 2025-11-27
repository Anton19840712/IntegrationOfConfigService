using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	/// <summary>
	/// Контроллер API для работы с журналами аудита системы.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Предоставляет API endpoints для поиска, фильтрации и анализа записей журнала аудита.
	/// Доступ к endpoints ограничен ролью "Admin" для обеспечения безопасности аудиторских данных.
	/// </para>
	/// <para>
	/// Журнал аудита содержит информацию о всех значимых действиях пользователей в системе
	/// для обеспечения подотчетности, отслеживания изменений и расследования инцидентов.
	/// </para>
	/// </remarks>
	/// <remarks>
	/// Инициализирует новый экземпляр класса <see cref="AuditLogsController"/>.
	/// </remarks>
	/// <param name="auditService">Сервис для работы с журналами аудита.</param>
	[ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin")] // Только администраторы могут просматривать аудит
    public class AuditLogsController(AuditLogService auditService) : ControllerBase
    {
        private readonly AuditLogService _auditService = auditService;

		/// <summary>
		/// Поиск по журналу аудита с фильтрацией и пагинацией.
		/// </summary>
		/// <param name="userId">Фильтр по идентификатору пользователя.</param>
		/// <param name="userLogin">Фильтр по логину пользователя.</param>
		/// <param name="action">Фильтр по типу действия.</param>
		/// <param name="fromDate">Фильтр по начальной дате.</param>
		/// <param name="toDate">Фильтр по конечной дате.</param>
		/// <param name="page">Номер страницы для пагинации (по умолчанию: 1).</param>
		/// <param name="pageSize">Размер страницы для пагинации (по умолчанию: 20).</param>
		/// <returns>
		/// Коллекция записей журнала аудита с метаданными пагинации в заголовках ответа.
		/// </returns>
		/// <response code="200">Записи журнала аудита успешно возвращены.</response>
		/// <response code="401">Пользователь не аутентифицирован.</response>
		/// <response code="403">Пользователь не имеет прав доступа к журналу аудита.</response>
		[HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid? userId,
            [FromQuery] string userLogin,
            [FromQuery] string action,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (logs, totalCount) = await _auditService.SearchLogsAsync(userId, userLogin, action, fromDate, toDate, page, pageSize);
            
            // Добавляем заголовки пагинации в ответ для удобства UI
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Number", page.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(logs);
        }

        /// <summary>
        /// Получение списка уникальных типов действий для фильтров.
        /// </summary>
        /// <returns>
        /// Коллекция уникальных типов действий, зафиксированных в журнале аудита.
        /// </returns>
        /// <response code="200">Список типов действий успешно возвращен.</response>
        /// <response code="401">Пользователь не аутентифицирован.</response>
        /// <response code="403">Пользователь не имеет прав доступа к журналу аудита.</response>
        [HttpGet("actions")]
        public async Task<IActionResult> GetActionTypes()
        {
            var actions = await _auditService.GetActionTypesAsync();
            return Ok(actions);
        }
        
        /// <summary>
        /// Получение списка подозрительных активностей по простым правилам.
        /// </summary>
        /// <returns>
        /// Коллекция подозрительных активностей, обнаруженных системой мониторинга.
        /// </returns>
        /// <response code="200">Список подозрительных активностей успешно возвращен.</response>
        /// <response code="401">Пользователь не аутентифицирован.</response>
        /// <response code="403">Пользователь не имеет прав доступа к журналу аудита.</response>
        [HttpGet("suspicious")]
        public async Task<IActionResult> GetSuspiciousActivity()
        {
            var activities = await _auditService.FindSuspiciousActivityAsync();
            return Ok(activities);
        }
    }
}