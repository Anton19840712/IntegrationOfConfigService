using ConfigurationService.Data;
using ConfigurationService.Domain;
using ConfigurationService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationService.Controllers;

/// <summary>
/// Контроллер для управления SIP конфигурациями
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigurationsController : ControllerBase
{
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<ConfigurationsController> _logger;

    /// <summary>
    /// Конструктор контроллера
    /// </summary>
    public ConfigurationsController(
        ConfigurationDbContext context,
        ILogger<ConfigurationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить все SIP конфигурации
    /// </summary>
    /// <returns>Список SIP конфигураций</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SipAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SipAccountDto>>> GetAll()
    {
        _logger.LogInformation("Получение списка всех SIP конфигураций");

        var accounts = await _context.SipAccounts
            .Select(a => MapToDto(a))
            .ToListAsync();

        return Ok(accounts);
    }

    /// <summary>
    /// Получить SIP конфигурацию по ID
    /// </summary>
    /// <param name="id">ID конфигурации</param>
    /// <returns>SIP конфигурация</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SipAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SipAccountDto>> GetById(int id)
    {
        _logger.LogInformation("Получение SIP конфигурации с ID {Id}", id);

        var account = await _context.SipAccounts.FindAsync(id);

        if (account == null)
        {
            _logger.LogWarning("SIP конфигурация с ID {Id} не найдена", id);
            return NotFound($"SIP конфигурация с ID {id} не найдена");
        }

        return Ok(MapToDto(account));
    }

    /// <summary>
    /// Получить SIP конфигурацию по user ID
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>SIP конфигурация пользователя</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(SipAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SipAccountDto>> GetByUserId(string userId)
    {
        _logger.LogInformation("Получение SIP конфигурации для пользователя {UserId}", userId);

        var account = await _context.SipAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (account == null)
        {
            _logger.LogWarning("SIP конфигурация для пользователя {UserId} не найдена", userId);
            return NotFound($"SIP конфигурация для пользователя {userId} не найдена");
        }

        return Ok(MapToDto(account));
    }

    /// <summary>
    /// Получить SIP конфигурацию с паролем по user ID (для внутреннего использования BFF)
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>SIP конфигурация пользователя с паролем</returns>
    [HttpGet("internal/user/{userId}")]
    [ProducesResponseType(typeof(SipAccountWithPasswordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SipAccountWithPasswordDto>> GetByUserIdWithPassword(string userId)
    {
        _logger.LogInformation("[INTERNAL] Получение SIP конфигурации с паролем для пользователя {UserId}", userId);

        var account = await _context.SipAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);

        if (account == null)
        {
            _logger.LogWarning("[INTERNAL] SIP конфигурация для пользователя {UserId} не найдена", userId);
            return NotFound($"SIP конфигурация для пользователя {userId} не найдена");
        }

        return Ok(MapToDtoWithPassword(account));
    }

    /// <summary>
    /// Получить список всех активных SIP номеров (для внутреннего использования BFF)
    /// </summary>
    /// <returns>Список SIP номеров</returns>
    [HttpGet("internal/all-accounts")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetAllAccountNumbers()
    {
        _logger.LogInformation("[INTERNAL] Получение списка всех активных SIP номеров");

        var accountNumbers = await _context.SipAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.SipAccountName)
            .Select(a => a.SipAccountName)
            .ToListAsync();

        _logger.LogInformation("[INTERNAL] Найдено {Count} активных SIP номеров", accountNumbers.Count);

        return Ok(accountNumbers);
    }

    /// <summary>
    /// Получить SIP конфигурацию пользователя + список доступных номеров для звонка (АГРЕГАЦИЯ для BFF)
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Конфигурация пользователя + доступные номера</returns>
    [HttpGet("internal/user/{userId}/with-available-numbers")]
    [ProducesResponseType(typeof(SipAccountWithAvailableNumbersDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SipAccountWithAvailableNumbersDto>> GetByUserIdWithAvailableNumbers(string userId)
    {
        _logger.LogInformation("[INTERNAL] Получение SIP конфигурации + доступных номеров для пользователя {UserId}", userId);

        // Запрос 1: Получаем конфиг текущего пользователя
        var currentAccount = await _context.SipAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);

        if (currentAccount == null)
        {
            _logger.LogWarning("[INTERNAL] SIP конфигурация для пользователя {UserId} не найдена", userId);
            return NotFound($"SIP конфигурация для пользователя {userId} не найдена");
        }

        // Запрос 2: Получаем все активные номера, кроме текущего пользователя
        var availableNumbers = await _context.SipAccounts
            .Where(a => a.IsActive && a.SipAccountName != currentAccount.SipAccountName)
            .OrderBy(a => a.SipAccountName)
            .Select(a => a.SipAccountName)
            .ToListAsync();

        _logger.LogInformation("[INTERNAL] Найдено {Count} доступных номеров для пользователя {AccountName}",
            availableNumbers.Count, currentAccount.SipAccountName);

        var result = new SipAccountWithAvailableNumbersDto
        {
            CurrentAccount = MapToDtoWithPassword(currentAccount),
            AvailableNumbers = availableNumbers
        };

        return Ok(result);
    }

    /// <summary>
    /// Создать новую SIP конфигурацию
    /// </summary>
    /// <param name="dto">Данные для создания</param>
    /// <returns>Созданная SIP конфигурация</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SipAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SipAccountDto>> Create([FromBody] CreateUpdateSipAccountDto dto)
    {
        _logger.LogInformation("Создание новой SIP конфигурации для пользователя {UserId}", dto.UserId);

        try
        {
            var account = new SipAccount
            {
                UserId = dto.UserId,
                SipAccountName = dto.SipAccountName,
                SipPassword = dto.SipPassword,
                SipDomain = dto.SipDomain,
                ProxyUri = dto.ProxyUri,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SipAccounts.Add(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("SIP конфигурация создана с ID {Id}", account.Id);

            return CreatedAtAction(nameof(GetById), new { id = account.Id }, MapToDto(account));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Ошибка при создании SIP конфигурации");
            return BadRequest("Ошибка при создании конфигурации. Возможно, SIP аккаунт с таким именем уже существует");
        }
    }

    /// <summary>
    /// Обновить существующую SIP конфигурацию
    /// </summary>
    /// <param name="id">ID конфигурации</param>
    /// <param name="dto">Данные для обновления</param>
    /// <returns>Обновленная SIP конфигурация</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SipAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SipAccountDto>> Update(int id, [FromBody] CreateUpdateSipAccountDto dto)
    {
        _logger.LogInformation("Обновление SIP конфигурации с ID {Id}", id);

        var account = await _context.SipAccounts.FindAsync(id);

        if (account == null)
        {
            _logger.LogWarning("SIP конфигурация с ID {Id} не найдена", id);
            return NotFound($"SIP конфигурация с ID {id} не найдена");
        }

        try
        {
            account.UserId = dto.UserId;
            account.SipAccountName = dto.SipAccountName;
            account.SipPassword = dto.SipPassword;
            account.SipDomain = dto.SipDomain;
            account.ProxyUri = dto.ProxyUri;
            account.IsActive = dto.IsActive;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("SIP конфигурация с ID {Id} успешно обновлена", id);

            return Ok(MapToDto(account));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении SIP конфигурации с ID {Id}", id);
            return BadRequest("Ошибка при обновлении конфигурации");
        }
    }

    /// <summary>
    /// Удалить SIP конфигурацию
    /// </summary>
    /// <param name="id">ID конфигурации</param>
    /// <returns>Результат удаления</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Удаление SIP конфигурации с ID {Id}", id);

        var account = await _context.SipAccounts.FindAsync(id);

        if (account == null)
        {
            _logger.LogWarning("SIP конфигурация с ID {Id} не найдена", id);
            return NotFound($"SIP конфигурация с ID {id} не найдена");
        }

        _context.SipAccounts.Remove(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SIP конфигурация с ID {Id} успешно удалена", id);

        return NoContent();
    }

    private static SipAccountDto MapToDto(SipAccount account)
    {
        return new SipAccountDto
        {
            Id = account.Id,
            UserId = account.UserId,
            SipAccountName = account.SipAccountName,
            SipDomain = account.SipDomain,
            ProxyUri = account.ProxyUri,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    private static SipAccountWithPasswordDto MapToDtoWithPassword(SipAccount account)
    {
        return new SipAccountWithPasswordDto
        {
            Id = account.Id,
            UserId = account.UserId,
            SipAccountName = account.SipAccountName,
            SipPassword = account.SipPassword,
            SipDomain = account.SipDomain,
            ProxyUri = account.ProxyUri,
            IsActive = account.IsActive
        };
    }

    /// <summary>
    /// Получить статус SIP аккаунта пользователя по userId
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Статус SIP аккаунта</returns>
    [HttpGet("user/{userId}/status")]
    [ProducesResponseType(typeof(SipAccountStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SipAccountStatusDto>> GetUserAccountStatus(string userId)
    {
        _logger.LogInformation("Получение статуса SIP аккаунта для пользователя {UserId}", userId);

        // Проверяем, есть ли назначенный SIP аккаунт
        var sipAccount = await _context.SipAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (sipAccount != null)
        {
            // У пользователя есть назначенный номер
            return Ok(new SipAccountStatusDto
            {
                Status = "assigned",
                UserId = userId,
                UserLogin = "", // TODO: получить из AuthService или передавать из BFF
                SipAccount = MapToDto(sipAccount),
                Message = "SIP account is assigned and ready to use"
            });
        }

        // Проверяем, есть ли pending assignment
        var pendingAssignment = await _context.PendingAssignments
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (pendingAssignment != null)
        {
            // Пользователь в очереди ожидания
            // Вычисляем позицию в очереди (используем ID вместо CreatedAt чтобы избежать проблем с DateTime.Kind)
            var pendingId = pendingAssignment.Id;
            var position = await _context.PendingAssignments
                .Where(p => p.Id <= pendingId)
                .CountAsync();

            return Ok(new SipAccountStatusDto
            {
                Status = "pending",
                UserId = userId,
                UserLogin = pendingAssignment.UserLogin,
                SipAccount = null,
                PendingPosition = position,
                PendingCreatedAt = pendingAssignment.CreatedAt,
                Message = $"SIP account assignment is pending. You are #{position} in queue. Your number will be assigned automatically when available."
            });
        }

        // У пользователя нет ни назначенного номера, ни pending assignment
        return Ok(new SipAccountStatusDto
        {
            Status = "not_requested",
            UserId = userId,
            UserLogin = "",
            SipAccount = null,
            Message = "No SIP account requested for this user"
        });
    }
}
