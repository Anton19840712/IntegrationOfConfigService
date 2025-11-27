using ConfigurationService.Data;
using ConfigurationService.Domain;
using ConfigurationService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationService.Controllers;

/// <summary>
/// Управление пулом доступных SIP номеров
/// </summary>
[ApiController]
[Route("api/sip-pool")]
public class SipPoolController(
	ConfigurationDbContext dbContext,
	ILogger<SipPoolController> logger) : ControllerBase
{
    private readonly ConfigurationDbContext _dbContext = dbContext;
    private readonly ILogger<SipPoolController> _logger = logger;

	/// <summary>
	/// Массовое добавление SIP номеров в пул
	/// </summary>
	/// <param name="request">Список номеров для добавления</param>
	/// <returns>Результат операции</returns>
	[HttpPost("bulk")]
    public async Task<ActionResult<BulkAddSipAccountsResponse>> BulkAddAccounts(
        [FromBody] BulkAddSipAccountsRequest request)
    {
        var response = new BulkAddSipAccountsResponse();

        foreach (var accountDto in request.Accounts)
        {
            // Проверяем существование
            var exists = await _dbContext.AvailableSipAccounts
                .AnyAsync(a => a.SipAccountName == accountDto.SipAccountName);

            if (exists)
            {
                response.SkippedCount++;
                response.SkippedAccounts.Add(accountDto.SipAccountName);
                _logger.LogWarning("SIP account {AccountName} already exists, skipping", accountDto.SipAccountName);
                continue;
            }

            // Добавляем в пул
            var account = new AvailableSipAccount
            {
                SipAccountName = accountDto.SipAccountName,
                SipPassword = accountDto.SipPassword,
                IsAssigned = false
            };

            _dbContext.AvailableSipAccounts.Add(account);
            response.AddedCount++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk add completed: {Added} added, {Skipped} skipped",
            response.AddedCount, response.SkippedCount);

        // После добавления проверяем pending assignments
        response.AutoAssignedCount = await ProcessPendingAssignmentsInternal();

        return Ok(response);
    }

    /// <summary>
    /// Получить список свободных SIP номеров
    /// </summary>
    /// <returns>Список свободных номеров</returns>
    [HttpGet("available")]
    public async Task<ActionResult<List<string>>> GetAvailableAccounts()
    {
        var available = await _dbContext.AvailableSipAccounts
            .Where(a => !a.IsAssigned)
            .Select(a => a.SipAccountName)
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(available);
    }

    /// <summary>
    /// Получить статистику пула
    /// </summary>
    /// <returns>Статистика</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<SipPoolStatsResponse>> GetStats()
    {
        var total = await _dbContext.AvailableSipAccounts.CountAsync();
        var assigned = await _dbContext.AvailableSipAccounts.CountAsync(a => a.IsAssigned);
        var pending = await _dbContext.PendingAssignments.CountAsync();

        var stats = new SipPoolStatsResponse
        {
            TotalAccounts = total,
            AssignedAccounts = assigned,
            AvailableAccounts = total - assigned,
            PendingAssignments = pending
        };

        return Ok(stats);
    }

    /// <summary>
    /// Получить список ожидающих назначений
    /// </summary>
    /// <returns>Список pending assignments</returns>
    [HttpGet("pending")]
    public async Task<ActionResult<List<object>>> GetPendingAssignments()
    {
        var pending = await _dbContext.PendingAssignments
            .OrderBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                p.UserLogin,
                p.DisplayName,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(pending);
    }

    /// <summary>
    /// Обработать ожидающие назначения (вручную)
    /// </summary>
    /// <returns>Количество обработанных</returns>
    [HttpPost("process-pending")]
    public async Task<ActionResult<int>> ProcessPendingAssignments()
    {
        var processed = await ProcessPendingAssignmentsInternal();

        _logger.LogInformation("Manually processed {Count} pending assignments", processed);

        return Ok(processed);
    }

    /// <summary>
    /// Добавить pending assignment (для тестирования)
    /// </summary>
    [HttpPost("pending")]
    public async Task<ActionResult> AddPendingAssignment([FromBody] AddPendingAssignmentRequest request)
    {
        var pending = new PendingAssignment
        {
            UserId = request.UserId,
            UserLogin = request.UserLogin,
            DisplayName = request.DisplayName,
            Status = "WaitingForAvailableAccount",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PendingAssignments.Add(pending);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Added pending assignment for user {UserLogin}", request.UserLogin);

        // Попытаться сразу обработать
        var processed = await ProcessPendingAssignmentsInternal();
        if (processed > 0)
        {
            _logger.LogInformation("Auto-processed {Count} pending assignments", processed);
        }

        return Ok(new { id = pending.Id, autoAssigned = processed > 0 });
    }

    /// <summary>
    /// Получить все SIP аккаунты из PostgreSQL (для отладки UI)
    /// </summary>
    [HttpGet("db/sip-accounts")]
    public async Task<ActionResult<object>> GetAllSipAccounts()
    {
        var sipAccounts = await _dbContext.SipAccounts
            .OrderBy(s => s.SipAccountName)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                s.SipAccountName,
                s.DisplayName,
                s.SipDomain,
                s.ProxyUri,
                s.IsActive,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(sipAccounts);
    }

    /// <summary>
    /// Получить все доступные SIP аккаунты из пула (для отладки UI)
    /// </summary>
    [HttpGet("db/available-accounts")]
    public async Task<ActionResult<object>> GetAllAvailableSipAccounts()
    {
        var availableAccounts = await _dbContext.AvailableSipAccounts
            .OrderBy(a => a.SipAccountName)
            .Select(a => new
            {
                a.Id,
                a.SipAccountName,
                a.IsAssigned,
                a.AssignedAt
            })
            .ToListAsync();

        return Ok(availableAccounts);
    }

    /// <summary>
    /// Получить все pending assignments из PostgreSQL (для отладки UI)
    /// </summary>
    [HttpGet("db/pending-assignments")]
    public async Task<ActionResult<object>> GetAllPendingAssignments()
    {
        var pendingAssignments = await _dbContext.PendingAssignments
            .OrderBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                p.UserLogin,
                p.DisplayName,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(pendingAssignments);
    }

    /// <summary>
    /// Обновить SIP аккаунт
    /// </summary>
    [HttpPut("db/sip-accounts/{id}")]
    public async Task<ActionResult> UpdateSipAccount(int id, [FromBody] UpdateSipAccountRequest request)
    {
        var sipAccount = await _dbContext.SipAccounts.FindAsync(id);
        if (sipAccount == null)
        {
            return NotFound($"SIP account with ID {id} not found");
        }

        // Обновляем только переданные поля
        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            sipAccount.DisplayName = request.DisplayName;
        }

        if (!string.IsNullOrEmpty(request.SipDomain))
        {
            sipAccount.SipDomain = request.SipDomain;
        }

        if (!string.IsNullOrEmpty(request.ProxyUri))
        {
            sipAccount.ProxyUri = request.ProxyUri;
        }

        if (!string.IsNullOrEmpty(request.UserId))
        {
            sipAccount.UserId = request.UserId;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated SIP account {SipAccountName} (ID: {Id})", sipAccount.SipAccountName, id);

        return Ok();
    }

    /// <summary>
    /// Удалить SIP аккаунт
    /// </summary>
    [HttpDelete("db/sip-accounts/{id}")]
    public async Task<ActionResult> DeleteSipAccount(int id)
    {
        var sipAccount = await _dbContext.SipAccounts.FindAsync(id);
        if (sipAccount == null)
        {
            return NotFound($"SIP account with ID {id} not found");
        }

        _dbContext.SipAccounts.Remove(sipAccount);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted SIP account {SipAccountName} (ID: {Id})", sipAccount.SipAccountName, id);

        return Ok();
    }

    /// <summary>
    /// Удалить доступный SIP аккаунт из пула
    /// </summary>
    [HttpDelete("db/available-accounts/{id}")]
    public async Task<ActionResult> DeleteAvailableAccount(int id)
    {
        var availableAccount = await _dbContext.AvailableSipAccounts.FindAsync(id);
        if (availableAccount == null)
        {
            return NotFound($"Available account with ID {id} not found");
        }

        _dbContext.AvailableSipAccounts.Remove(availableAccount);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted available account {SipAccountName} (ID: {Id})", availableAccount.SipAccountName, id);

        return Ok();
    }

    /// <summary>
    /// Удалить pending assignment
    /// </summary>
    [HttpDelete("db/pending-assignments/{id}")]
    public async Task<ActionResult> DeletePendingAssignment(int id)
    {
        var pendingAssignment = await _dbContext.PendingAssignments.FindAsync(id);
        if (pendingAssignment == null)
        {
            return NotFound($"Pending assignment with ID {id} not found");
        }

        _dbContext.PendingAssignments.Remove(pendingAssignment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted pending assignment for user {UserLogin} (ID: {Id})", pendingAssignment.UserLogin, id);

        return Ok();
    }

    /// <summary>
    /// Внутренний метод обработки pending assignments
    /// </summary>
    private async Task<int> ProcessPendingAssignmentsInternal()
    {
        var pending = await _dbContext.PendingAssignments
            .Where(p => p.Status == "WaitingForAvailableAccount")
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        int processedCount = 0;

        foreach (var assignment in pending)
        {
            // Получаем список уже назначенных SIP account names
            var assignedAccountNames = await _dbContext.SipAccounts
                .Select(s => s.SipAccountName)
                .ToListAsync();

            // Ищем свободный номер, который еще не назначен ни в available_sip_accounts, ни в sip_accounts
            var availableAccount = await _dbContext.AvailableSipAccounts
                .Where(a => !a.IsAssigned && !assignedAccountNames.Contains(a.SipAccountName))
                .OrderBy(a => a.SipAccountName)
                .FirstOrDefaultAsync();

            if (availableAccount == null)
            {
                _logger.LogWarning("No available accounts left to process pending assignments");
                break;
            }

            // Создаем SIP аккаунт
            var sipAccount = new SipAccount
            {
                UserId = assignment.UserId,
                SipAccountName = availableAccount.SipAccountName,
                SipPassword = availableAccount.SipPassword,
                DisplayName = assignment.DisplayName,
                SipDomain = "sip.pbx", // TODO: из конфига
                ProxyUri = "sip:172.16.211.135:5060", // TODO: из конфига
                IsActive = true
            };

            _dbContext.SipAccounts.Add(sipAccount);

            // Помечаем номер как назначенный
            availableAccount.IsAssigned = true;
            availableAccount.AssignedAt = DateTime.UtcNow;

            // Удаляем из pending
            _dbContext.PendingAssignments.Remove(assignment);

            processedCount++;

            _logger.LogInformation(
                "Auto-assigned SIP account {AccountName} to pending user {UserId}",
                availableAccount.SipAccountName, assignment.UserId);

            // Сохраняем изменения после каждого назначения, чтобы избежать дубликатов в следующей итерации
            await _dbContext.SaveChangesAsync();
        }

        return processedCount;
    }
}
