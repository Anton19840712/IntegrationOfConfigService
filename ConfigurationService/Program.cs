using ConfigurationService.Data;
using ConfigurationService.Events;
using ConfigurationService.Services;
using ConfigurationService.Services.Eureka;
using ConfigurationService.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.PostgreSQL;
using Npgsql;
using NpgsqlTypes;
using SipIntegration.EventBus.RabbitMQ.Extensions;
using SipIntegration.EventBus.RabbitMQ.Consumer;

Console.Title = "⚙️ Config Service";

/// <summary>
/// Точка входа приложения ConfigurationService
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// Serilog - Централизованное логирование в PostgreSQL
// ═══════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ConfigurationService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    // Console вывод настроен в appsettings.Development.json
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("LogsDatabase")
            ?? "Host=postgres-config;Database=logsdb;Username=postgres;Password=postgres",
        tableName: "logs",
        needAutoCreateTable: true,
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
            { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            { "timestamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
            { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
            { "log_event", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
            { "service_name", new SinglePropertyColumnWriter("Service", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            { "environment", new SinglePropertyColumnWriter("Environment", PropertyWriteMethod.ToString, NpgsqlDbType.Text) }
        })
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    // Добавление сервисов в контейнер DI
    builder.Services.AddControllers();
    builder.Services.AddHttpClient(); // Для EurekaRegistrationService

    // FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateUpdateSipAccountDtoValidator>();

    // Entity Framework Core + PostgreSQL
    builder.Services.AddDbContext<ConfigurationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "ConfigurationService API",
            Version = "v1",
            Description = "Микросервис управления SIP конфигурациями пользователей"
        });

        // Подключение XML комментариев
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Health Checks - проверяем критичные компоненты
    var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, name: "database")
        .AddRabbitMQ(
            rabbitConnectionString: $"amqp://{rabbitMqConfig["UserName"]}:{rabbitMqConfig["Password"]}@{rabbitMqConfig["HostName"]}:{rabbitMqConfig["Port"]}/{rabbitMqConfig["VirtualHost"]}",
            name: "rabbitmq",
            tags: new[] { "messaging" });

    // Eureka Service Discovery (опционально)
    var eurekaEnabled = builder.Configuration.GetValue<bool>("Eureka:Enabled", false);
    if (eurekaEnabled)
    {
        healthChecksBuilder.AddUrlGroup(
            new Uri(builder.Configuration["Eureka:ServerUrl"] + "/apps" ?? "http://localhost:8761/eureka/apps"),
            name: "eureka",
            tags: new[] { "discovery" });
        builder.Services.AddHostedService<EurekaRegistrationService>();
    }

    // ═══════════════════════════════════════════════════════════
    // RabbitMQ - Event-Driven Architecture
    // ═══════════════════════════════════════════════════════════

    // Регистрируем RabbitMQ EventBus (Publisher) из NuGet пакета
    builder.Services.AddRabbitMqEventBus(options =>
    {
        options.HostName = builder.Configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost";
        options.UserName = builder.Configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest";
        options.Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
        options.VirtualHost = builder.Configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/";
        options.Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
        options.ExchangeName = "configservice.events";
    });

    // Регистрируем RabbitMQ Consumer из NuGet пакета v2.0.0
    builder.Services.AddRabbitMqConsumer(options =>
    {
        options.HostName = builder.Configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost";
        options.UserName = builder.Configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest";
        options.Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
        options.VirtualHost = builder.Configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/";
        options.Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
    });

    // Регистрируем Event Handler как Scoped (использует DbContext)
    builder.Services.AddEventHandler<UserCreatedEvent, UserCreatedEventHandler>();

    var app = builder.Build();

    // ═══════════════════════════════════════════════════════════
    // Подписка на события из RabbitMQ
    // ═══════════════════════════════════════════════════════════
    var consumer = app.Services.GetRequiredService<RabbitMqEventConsumer>();
    consumer.Subscribe<UserCreatedEvent>(
        exchangeName: "authservice.events",
        queueName: "configservice.user.created");

    // Проверка подключения к БД и получение статистики SIP пула (без логирования SQL)
    int freeAccounts = 0, assignedAccounts = 0, pendingUsers = 0;
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        try
        {
            var canConnect = dbContext.Database.CanConnect();

            if (!canConnect)
            {
                Log.Error("Не удалось подключиться к базе данных");
                throw new Exception("Database connection failed");
            }

            // Статистика SIP пула (AsNoTracking + тихий запрос)
            freeAccounts = dbContext.AvailableSipAccounts.AsNoTracking().Count(a => !a.IsAssigned);
            assignedAccounts = dbContext.AvailableSipAccounts.AsNoTracking().Count(a => a.IsAssigned);
            pendingUsers = dbContext.PendingAssignments.AsNoTracking().Count();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при подключении к базе данных");
            throw;
        }
    }

    // Middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ConfigurationService API v1");
            options.RoutePrefix = string.Empty; // Swagger UI на корневом URL
        });
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.00} ms";
    });

    // Static files для monitor page
    app.UseStaticFiles();

    app.UseAuthorization();

    app.MapControllers();

    // Health Check endpoint
    app.MapHealthChecks("/health");

    // Startup Banner (использует Console.WriteLine для чистого вывода без Serilog)
    var port = builder.Configuration["ServiceDiscovery:ServicePort"] ?? "5029";
    var dbName = builder.Configuration.GetConnectionString("DefaultConnection")?.Split(";")
        .FirstOrDefault(s => s.StartsWith("Database="))?.Split("=")[1] ?? "configuration_db";

    Console.WriteLine("==================================================");
    Console.WriteLine("ConfigurationService успешно запущен!");
    Console.WriteLine($"Окружение: {app.Environment.EnvironmentName}");
    Console.WriteLine($"Swagger UI: http://localhost:{port}");
    Console.WriteLine($"Health: http://localhost:{port}/health");
    Console.WriteLine($"SIP Pool Monitor: http://localhost:{port}/sip-pool-monitor.html");
    Console.WriteLine($"База данных: {dbName}");
    Console.WriteLine($"Eureka: {(eurekaEnabled ? "Enabled" : "Disabled")}");
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine("SIP Pool start statistics:");
    Console.WriteLine($"  Свободных аккаунтов: {freeAccounts}");
    Console.WriteLine($"  Назначенных аккаунтов: {assignedAccounts}");
    Console.WriteLine($"  Ожидают назначения: {pendingUsers}");
    Console.WriteLine("--------------------------------------------------");
    Console.WriteLine($"Время запуска: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine("==================================================");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ConfigurationService завершился с критической ошибкой");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
