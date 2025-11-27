using ConfigurationService.Data;
using ConfigurationService.Domain;
using ConfigurationService.Events;
using Microsoft.EntityFrameworkCore;
using SipIntegration.EventBus.RabbitMQ.Abstractions;

namespace ConfigurationService.Services;

/// <summary>
/// Реализация обработчика события создания пользователя
/// Автоматически назначает SIP номер из пула или создает pending assignment
/// </summary>
public class UserCreatedEventHandler : IUserCreatedEventHandler
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IEventBus _eventBus;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IEventBus eventBus)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _eventBus = eventBus;
    }

    public async Task<bool> HandleAsync(UserCreatedEvent userCreatedEvent)
    {
        try
        {
            _logger.LogInformation(
                "[UserCreatedHandler] Обработка события создания пользователя: UserId={UserId}, Login={Login}",
                userCreatedEvent.UserId, userCreatedEvent.UserLogin);

            // Создаем scope для получения DbContext (scoped dependency)
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

            // Проверяем, есть ли уже SIP аккаунт для этого пользователя
            var existingAccount = await dbContext.SipAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userCreatedEvent.UserId);

            if (existingAccount != null)
            {
                _logger.LogInformation(
                    "[UserCreatedHandler] SIP аккаунт уже существует для UserId={UserId}: {SipAccountName}",
                    userCreatedEvent.UserId, existingAccount.SipAccountName);
                return true;
            }

            // ВАЖНО: Проверяем наличие pending assignments
            // Если очередь НЕ пуста, новый пользователь должен встать в конец очереди (FIFO)
            var hasPendingAssignments = await dbContext.PendingAssignments
                .AnyAsync(p => p.Status == "WaitingForAvailableAccount");

            if (hasPendingAssignments)
            {
                // Есть ожидающие в очереди - новый пользователь тоже идет в очередь
                await CreatePendingAssignmentAsync(dbContext, userCreatedEvent);

                _logger.LogInformation(
                    "[UserCreatedHandler] Пользователь {Login} добавлен в очередь pending (есть {Count} ожидающих)",
                    userCreatedEvent.UserLogin,
                    await dbContext.PendingAssignments.CountAsync());

                return true;
            }

            // Очередь пуста - пытаемся назначить свободный номер напрямую
            var assigned = await TryAssignSipAccountAsync(dbContext, userCreatedEvent);

            if (assigned)
            {
                _logger.LogInformation(
                    "[UserCreatedHandler] SIP номер успешно назначен для UserId={UserId}",
                    userCreatedEvent.UserId);
                return true;
            }

            // Пул пуст, создаем pending assignment
            await CreatePendingAssignmentAsync(dbContext, userCreatedEvent);

            _logger.LogWarning(
                "[UserCreatedHandler] Пул SIP номеров пуст. Создан pending assignment для UserId={UserId}",
                userCreatedEvent.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[UserCreatedHandler] Ошибка при обработке UserCreatedEvent для UserId={UserId}",
                userCreatedEvent.UserId);
            return false; // Вернем false для retry
        }
    }

    /// <summary>
    /// Попытка назначить свободный SIP номер из пула
    /// </summary>
    private async Task<bool> TryAssignSipAccountAsync(
        ConfigurationDbContext dbContext,
        UserCreatedEvent userCreatedEvent)
    {
        // Начинаем транзакцию для атомарности операции
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Ищем первый свободный номер
            var availableAccount = await dbContext.AvailableSipAccounts
                .Where(a => !a.IsAssigned)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (availableAccount == null)
            {
                return false; // Пул пуст
            }

            // Формируем display_name из FirstName LastName
            var displayName = $"{userCreatedEvent.FirstName} {userCreatedEvent.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = userCreatedEvent.UserLogin;
            }

            // Получаем настройки SIP из конфигурации
            var sipDomain = _configuration["Sip:Domain"] ?? "sip.pbx";
            var proxyUri = _configuration["Sip:ProxyUri"] ?? "sip:172.16.211.135:5060";

            // Создаем SIP аккаунт
            var sipAccount = new SipAccount
            {
                UserId = userCreatedEvent.UserId,
                SipAccountName = availableAccount.SipAccountName,
                SipPassword = availableAccount.SipPassword,
                DisplayName = displayName,
                SipDomain = sipDomain,
                ProxyUri = proxyUri,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.SipAccounts.Add(sipAccount);

            // Помечаем номер как назначенный
            availableAccount.IsAssigned = true;
            availableAccount.AssignedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "[UserCreatedHandler] SIP аккаунт {SipAccountName} назначен пользователю {UserId} ({DisplayName})",
                availableAccount.SipAccountName, userCreatedEvent.UserId, displayName);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[UserCreatedHandler] Ошибка при назначении SIP аккаунта");
            throw;
        }
    }

    /// <summary>
    /// Создание pending assignment когда пул пуст
    /// </summary>
    private async Task CreatePendingAssignmentAsync(
        ConfigurationDbContext dbContext,
        UserCreatedEvent userCreatedEvent)
    {
        // Проверяем, нет ли уже pending assignment для этого пользователя
        var existingPending = await dbContext.PendingAssignments
            .FirstOrDefaultAsync(pa => pa.UserId == userCreatedEvent.UserId);

        if (existingPending != null)
        {
            _logger.LogInformation(
                "[UserCreatedHandler] Pending assignment уже существует для UserId={UserId}",
                userCreatedEvent.UserId);
            return;
        }

        var displayName = $"{userCreatedEvent.FirstName} {userCreatedEvent.LastName}".Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = userCreatedEvent.UserLogin;
        }

        var pendingAssignment = new PendingAssignment
        {
            UserId = userCreatedEvent.UserId,
            UserLogin = userCreatedEvent.UserLogin,
            DisplayName = displayName,
            Status = "WaitingForAvailableAccount",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PendingAssignments.Add(pendingAssignment);
        await dbContext.SaveChangesAsync();

        // Публикация события SipAccountPendingCreated в RabbitMQ
        // Вычисляем позицию в очереди
        var queuePosition = await dbContext.PendingAssignments
            .Where(p => p.Id <= pendingAssignment.Id)
            .CountAsync();

        var pendingCreatedEvent = new SipAccountPendingCreated
        {
            UserId = userCreatedEvent.UserId,
            UserLogin = userCreatedEvent.UserLogin,
            DisplayName = displayName,
            QueuePosition = queuePosition,
            CreatedAt = pendingAssignment.CreatedAt,
            Timestamp = DateTime.UtcNow,
            Message = $"User {userCreatedEvent.UserLogin} added to pending queue at position {queuePosition}"
        };

        // Публикуем событие в RabbitMQ для NotificationService
        await _eventBus.PublishAsync(pendingCreatedEvent);

        _logger.LogInformation(
            "[UserCreatedHandler] Опубликовано событие SipAccountPendingCreated для UserId={UserId}",
            userCreatedEvent.UserId);

        _logger.LogInformation(
            "[UserCreatedHandler] Pending assignment создан для UserId={UserId}, Login={Login}, позиция в очереди: {QueuePosition}",
            userCreatedEvent.UserId, userCreatedEvent.UserLogin, queuePosition);
    }
}
