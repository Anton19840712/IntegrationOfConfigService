using Application.DTOs.Requests;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для управления сервисными клиентами (machine-to-machine аутентификация)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Предоставляет функционал для создания и управления сервисными клиентами, которые используются
    /// для аутентификации между сервисами (machine-to-machine). Сервисные клиенты используют
    /// ClientId/ClientSecret для получения токенов доступа.
    /// </para>
    /// <para>
    /// <b>Требования безопасности:</b> Управление клиентами доступно только пользователям с ролью "Admin".
    /// Endpoint /token доступен анонимно для аутентификации клиентов. ClientSecret возвращается только
    /// один раз при создании клиента.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/service-clients")]
    [Authorize(Roles = "Admin")]
    public class ServiceClientsController : ControllerBase
    {
        private readonly ServiceClientService _service;
        private readonly ILogger<ServiceClientsController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр контроллера управления сервисными клиентами
        /// </summary>
        /// <param name="service">Сервис для работы с сервисными клиентами</param>
        /// <param name="logger">Логгер для записи событий и ошибок</param>
        public ServiceClientsController(ServiceClientService service, ILogger<ServiceClientsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Получает список всех зарегистрированных сервисных клиентов
        /// </summary>
        /// <returns>Коллекция сервисных клиентов без секретной информации</returns>
        /// <response code="200">Успешное получение списка клиентов</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Запрос списка сервисных клиентов пользователем {UserId}", currentUserId);

            var clients = await _service.GetAllAsync();
            var result = clients.Select(c => new
            {
                c.Id,
                c.Name,
                c.ClientId,
                c.IsActive,
                c.CreatedAt,
                c.LastUsedAt
            });

            _logger.LogInformation("Возвращено {ClientCount} сервисных клиентов", result.Count());

            return Ok(result);
        }

        /// <summary>
        /// Создает нового сервисного клиента
        /// </summary>
        /// <param name="dto">DTO с данными для создания клиента</param>
        /// <returns>Данные созданного клиента, включая ClientSecret (возвращается только один раз!)</returns>
        /// <response code="200">Клиент успешно создан</response>
        /// <response code="400">Неверные данные запроса</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateServiceClientRequest dto)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Создание сервисного клиента с именем '{ClientName}' пользователем {UserId}",
                dto.Name, currentUserId);

            var (id, secret) = await _service.CreateAsync(dto.Name);

            _logger.LogInformation("Сервисный клиент '{ClientName}' успешно создан с ID {ClientId}",
                dto.Name, id);

            return Ok(new { clientId = id, clientSecret = secret });
        }

        /// <summary>
        /// Удаляет сервисного клиента из системы
        /// </summary>
        /// <param name="id">GUID идентификатор клиента</param>
        /// <response code="204">Клиент успешно удален</response>
        /// <response code="404">Клиент с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Удаление сервисного клиента {ClientId} пользователем {UserId}",
                id, currentUserId);

            await _service.DeleteAsync(id);

            _logger.LogInformation("Сервисный клиент {ClientId} успешно удален", id);

            return NoContent();
        }

        /// <summary>
        /// Деактивирует сервисного клиента (блокирует доступ)
        /// </summary>
        /// <param name="id">GUID идентификатор клиента</param>
        /// <response code="204">Клиент успешно деактивирован</response>
        /// <response code="404">Клиент с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Деактивация сервисного клиента {ClientId} пользователем {UserId}",
                id, currentUserId);

            await _service.DeactivateAsync(id);

            _logger.LogInformation("Сервисный клиент {ClientId} успешно деактивирован", id);

            return NoContent();
        }

        /// <summary>
        /// Активирует ранее деактивированного сервисного клиента
        /// </summary>
        /// <param name="id">GUID идентификатор клиента</param>
        /// <response code="204">Клиент успешно активирован</response>
        /// <response code="404">Клиент с указанным ID не найден</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Отсутствует роль Admin</response>
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Активация сервисного клиента {ClientId} пользователем {UserId}",
                id, currentUserId);

            await _service.ActivateAsync(id);

            _logger.LogInformation("Сервисный клиент {ClientId} успешно активирован", id);

            return NoContent();
        }

        /// <summary>
        /// Аутентифицирует сервисного клиента и возвращает токен доступа
        /// </summary>
        /// <param name="dto">DTO с учетными данными клиента</param>
        /// <returns>JWT токен доступа для сервисного клиента</returns>
        /// <response code="200">Успешная аутентификация, возвращен токен</response>
        /// <response code="401">Неверные учетные данные или клиент деактивирован</response>
        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] AuthenticateServiceClientRequest dto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Попытка аутентификации сервисного клиента {ClientId} с IP {IpAddress}",
                dto.ClientId, ipAddress);

            try
            {
                var token = await _service.AuthenticateAsync(dto.ClientId, dto.ClientSecret, ipAddress);

                _logger.LogInformation("Успешная аутентификация сервисного клиента {ClientId}", dto.ClientId);

                return Ok(new { accessToken = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Неудачная аутентификация сервисного клиента {ClientId}: {ErrorMessage}",
                    dto.ClientId, ex.Message);

                return Unauthorized(new { message = "Неверные учётные данные клиента" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при аутентификации сервисного клиента {ClientId}", dto.ClientId);

                return Unauthorized(new { message = "Неверные учётные данные клиента" });
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
