using API.Middlewares;
using API.Services.Eureka;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using SipIntegration.EventBus.RabbitMQ.Extensions;
using SipIntegration.Tarantool.Extensions;
using Application.ServiceMessaging;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;
using Application.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Application.Interfaces.Repository;
using Application.Interfaces.Service;
using Application.Validators.Users;
using Serilog.Events;
using Microsoft.Extensions.Options;

// Установка UTF-8 кодировки для консоли (для правильного отображения эмодзи и специальных символов)
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Title = "🔐 Auth Service";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry(options =>
{
    options.Dsn = "https://73ba5d91a3a0a89aa0674a2ed5ac203b@sentry.pit.protei.ru/2";
    options.TracesSampleRate = 1.0;
    options.Debug = false;
    options.CaptureFailedRequests = true;
    options.SendDefaultPii = true;
    options.Experimental.EnableLogs = true;
    options.ProfilesSampleRate = 1.0;
    options.AddProfilingIntegration();
});

// --- ������������ Serilog (������) ---
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Sentry(o =>
    {
        o.Dsn = "https://73ba5d91a3a0a89aa0674a2ed5ac203b@sentry.pit.protei.ru/2";
        o.TracesSampleRate = 1.0;
        o.Debug = false;
        // Debug and higher are stored as breadcrumbs (default is Information)
        o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
        // Warning and higher is sent as event (default is Error)
        o.MinimumEventLevel = LogEventLevel.Warning;
    })
    .CreateLogger();

builder.Host.UseSerilog();


// Тут поддержка получения конфигурации
//builder.Configuration.AddRemoteConfiguration(builder.Services, options =>
//{
//    // Адрес ConfigService из appsettings.json
//    options.ConfigurationServiceUri = new Uri(builder.Configuration["ConfigurationService:Url"]!);

//    // Уникальное имя текущего сервиса
//    options.ServiceName = "auth-service";

//    // Текущее окружение (Production, Development, и т.д.)
//    options.EnvironmentName = builder.Environment.EnvironmentName;

//    // API-ключ из appsettings.json
//    options.ApiKey = builder.Configuration["ConfigurationService:ApiKey"]!;

//    // (Опционально) Если true, приложение запустится, даже если ConfigService недоступен
//    options.Optional = true;

//    // Включаем обновление через RabbitMQ
//    options.UseRabbitMqForUpdates = true;

//    // Строка подключения к RabbitMQ из appsettings.json
//    options.RabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq");
//});


// Добавляем стандартные источники конфигурации
//builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//                    .AddEnvironmentVariables()
//                    .AddDockerSecrets();  //Добавляем поддержку Docker Swarm secrets. По умолчанию читает из /run/secrets и добавляет как конфиг

// --- ���������� DbContext � �������� ---

//var secretValue = builder.Configuration["TestSettings:MySecretValue"];
//Console.WriteLine($"TestSettings:MySecretValue = {secretValue}");

builder.Services.AddDbContextPool<AuthDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        //Для разделённых запросов
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

        // Увеличиваем таймаут команд до 5 минут (300 секунд) для миграций
        npgsqlOptions.CommandTimeout(300);
    });
});

// --- ����������� ������������ ---

// --- Tarantool Infrastructure (for User Cache and Rate Limiting) ---
var tarantoolEnabled = builder.Configuration.GetValue<bool>("Tarantool:Enabled", false);

// Cache Settings (feature toggles)
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("Cache"));

var cacheSettings = builder.Configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();

// Always register Tarantool (either real or no-op)
if (tarantoolEnabled)
{
    builder.Services.AddTarantool(builder.Configuration);
    Log.Information("✓ Tarantool connection configured");
}
else
{
    builder.Services.AddSingleton<SipIntegration.Tarantool.Abstractions.ITarantoolConnection, Infrastructure.Services.NoOpTarantoolConnection>();
    Log.Information("● Tarantool connection not configured (no-op)");
}

// Register TarantoolConnectionManager for dynamic enable/disable
builder.Services.AddSingleton(sp =>
{
    var connection = sp.GetRequiredService<SipIntegration.Tarantool.Abstractions.ITarantoolConnection>();
    var logger = sp.GetRequiredService<ILogger<Infrastructure.Services.TarantoolConnectionManager>>();
    return new Infrastructure.Services.TarantoolConnectionManager(connection, logger, tarantoolEnabled);
});

Log.Information("  → User Cache: {Enabled} (TTL: {TTL}s)",
    cacheSettings.UserCacheEnabled, cacheSettings.UserCacheTtlSeconds);
Log.Information("  → Login Rate Limiting: {Enabled}",
    cacheSettings.LoginRateLimitingEnabled);
Log.Information("  → Dynamic Tarantool Control: Enabled (initial state: {InitialState})",
    tarantoolEnabled ? "ON" : "OFF");

// Always use CachedUserRepository (checks TarantoolConnectionManager at runtime)
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(sp =>
{
    var innerRepository = sp.GetRequiredService<UserRepository>();
    var tarantoolConnection = sp.GetRequiredService<SipIntegration.Tarantool.Abstractions.ITarantoolConnection>();
    var logger = sp.GetRequiredService<ILogger<CachedUserRepository>>();
    var cacheSettingsOptions = sp.GetRequiredService<IOptions<CacheSettings>>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var tarantoolManager = sp.GetRequiredService<Infrastructure.Services.TarantoolConnectionManager>();

    return new CachedUserRepository(innerRepository, tarantoolConnection, logger, cacheSettingsOptions, httpContextAccessor, tarantoolManager);
});

Log.Information("✓ UserRepository configured with dynamic Tarantool support");

// Always use LoginRateLimiter (checks TarantoolConnectionManager at runtime)
builder.Services.AddScoped<ILoginRateLimiter, Infrastructure.Services.LoginRateLimiter>();
Log.Information("✓ LoginRateLimiter configured with dynamic Tarantool support");

// Other repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPrivilegeRepository, PrivilegeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, PostgresRefreshTokenRepository>();

Log.Information("✓ RefreshTokenRepository configured with PostgreSQL");

builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IRolePrivilegeRepository, RolePrivilegeRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IServiceClientRepository, ServiceClientRepository>();
builder.Services.AddScoped<IUserBehaviorRepository, UserBehaviorRepository>();
builder.Services.AddScoped<IUserBehaviorAnalyzer, UserBehaviorAnalyzer>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddSingleton<IDataEncryptor, AesEncryptor>();


// Фоновый сервис отчистики устаревших refresh токенов
builder.Services.Configure<RefreshTokenCleanupSettings>(
    builder.Configuration.GetSection("BackgroundServices:RefreshTokenCleanup"));

// Регистрируем фоновый сервис
builder.Services.AddHostedService<RefreshTokenCleanupService>();

// --- ����������� �������� Application Layer ---
builder.Services.AddScoped<Application.Services.AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserService>(); // Регистрация конкретного класса для контроллера
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PrivilegeService>();
builder.Services.AddScoped<ServiceClientService>();
builder.Services.AddScoped<AuthDbSeeder>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IpAddressHelper>();
builder.Services.AddScoped<AuditLogService>();

builder.Services.AddRabbitMqEventBus(options =>
{
    var config = builder.Configuration.GetSection("RabbitMQ");
    options.HostName = config.GetValue<string>("HostName")
        ?? throw new InvalidOperationException("RabbitMQ:HostName is not configured in appsettings");
    options.UserName = config.GetValue<string>("UserName")
        ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured in appsettings");
    options.Password = config.GetValue<string>("Password")
        ?? throw new InvalidOperationException("RabbitMQ:Password is not configured in appsettings");
    options.VirtualHost = config.GetValue<string>("VirtualHost")
        ?? throw new InvalidOperationException("RabbitMQ:VirtualHost is not configured in appsettings");
    options.Port = config.GetValue<int?>("Port")
        ?? throw new InvalidOperationException("RabbitMQ:Port is not configured in appsettings");
    options.ExchangeName = config.GetValue<string>("ExchangeName") ?? "authservice.events";
    options.ExchangeType = config.GetValue<string>("ExchangeType") ?? "topic";
});
builder.Services.AddHostedService<RabbitMqReconnectService>();


// ����������� PasswordHasher ��� User � ServiceClient
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// --- ����������� JwtTokenGenerator ---
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// --- ��������� JWT Authentication ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// --- ��������� Authorization ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalPolicy", policy =>
        policy.RequireClaim("scope", "internal_access"));

    options.AddPolicy("AdminOrInternal", policy =>
         policy.RequireAssertion(context =>
             context.User.IsInRole("Admin") ||
             context.User.HasClaim("scope", "internal_access")
         ));
});


// --- HttpClient для внешних сервисов ---
builder.Services.AddHttpClient();

// --- ���������� ������������ � Swagger ---
builder.Services.AddControllers(options =>
{
    // Фильтр для ошибок
    options.Filters.Add<API.Filters.ValidationFilter>();
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SRUB AuthService",
        Version = "v1.3",
        Description = "API для аутентификации и авторизации",
        Contact = new OpenApiContact
        {
            Name = "Protei IT",
            Email = "info@pit.protei.ru",
            Url = new Uri("https://pit.protei.ru")
        },
        License = new OpenApiLicense
        {
            Name = "SRUB License",
            Url = new Uri("https://pit.protei.ru")
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Заголовок авторизации JWT с использованием схемы Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);                                                      //Описание в основном проекте
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Application.xml"));    //Описание из Application
});


// CORS Configuration
var corsSection = builder.Configuration.GetSection("Cors");
var allowAnyOrigin = corsSection.GetValue<bool>("AllowAnyOrigin");
var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (allowAnyOrigin)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Включить сжатие для HTTPS
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest; // или CompressionLevel.Optimal
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest; // или CompressionLevel.Optimal
});

// Eureka Service Discovery (опционально)
var eurekaEnabled = builder.Configuration.GetValue<bool>("Eureka:Enabled", false);
if (eurekaEnabled)
{
    builder.Services.AddHostedService<EurekaRegistrationService>();
    Log.Information("✓ Eureka service discovery enabled");
}
else
{
    Log.Information("● Eureka service discovery disabled");
}

// Регистрируем health checks
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
var healthChecksBuilder = builder.Services.AddHealthChecks()
    // Проверка подключения к БД
    .AddDbContextCheck<AuthDbContext>(
        name: "auth-database",
        tags: new[] { "database", "ready" })

    // Проверка RabbitMQ
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitMqConfig["UserName"]}:{rabbitMqConfig["Password"]}@{rabbitMqConfig["HostName"]}:{rabbitMqConfig["Port"]}/{rabbitMqConfig["VirtualHost"]}",
        name: "rabbitmq",
        tags: new[] { "messaging" });

// Проверка Eureka (опционально)
if (eurekaEnabled)
{
    var eurekaServerUrl = builder.Configuration["Eureka:ServerUrl"] ?? "http://localhost:8761/eureka";
    healthChecksBuilder.AddUrlGroup(
        new Uri($"{eurekaServerUrl}/apps"),
        name: "eureka",
        tags: new[] { "discovery" });
}

// TODO: Добавить health check для Tarantool через отдельный класс
// .AddCheck<TarantoolHealthCheck>("tarantool-connection", tags: new[] { "tarantool", "cache" })

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Указываем, какие заголовки будет обрабатывать приложение
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // ВАЖНО: Укажите здесь IP-адреса ваших reverse-proxy.
    // Это говорит приложению, что оно может доверять заголовкам, приходящим с этих IP.
    // 10.0.0.2 - это IP шлюза Docker Swarm, который часто выступает как прокси.
    // Добавьте сюда IP-адреса всех ваших Nginx/Traefik/API Gateway.
    // Можно указывать подсети.
    options.KnownProxies.Add(IPAddress.Parse("::ffff:10.0.0.2"));
    options.KnownProxies.Add(IPAddress.Parse("10.0.0.2")); // На всякий случай и в формате IPv4
    
    // Если у вас несколько прокси в сети, можно добавить всю подсеть
    // options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
});

var app = builder.Build();

#region Получаем информацию об окружении приложения
var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? assembly.GetName().Version?.ToString()
    ?? "Unknown";
var appName = assembly.GetName().Name ?? "SRUB CardSystem";
var environment = app.Environment.EnvironmentName;
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "https://localhost:7000;http://localhost:5000";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//var featuresSettings = app.Services.GetRequiredService<IOptions<FeaturesSettings>>().Value;
#endregion


// --- Вызов миграций и сидера при старте ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    // 1. Применяем миграции => создаём/обновляем таблицы
    await db.Database.MigrateAsync();

    // 2. Запуск сидера для наполнения начальными данными
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
    await seeder.SeedAsync();
}

app.UseCors("AllowAll");
app.UseForwardedHeaders();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseSerilogRequestLogging();
app.UseSecurityHeaders();

// Response compression only in Production (to avoid Browser Link warnings in Development)
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}
app.UseHttpsRedirection();
app.UseHsts();
app.UseMiddleware<ValidationExceptionMiddleware>();

// Static files for test page
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Настраиваем endpoint для health checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    // Группируем проверки по тегам
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data
            }),
            timestamp = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    // Все проверки
    Predicate = (_) => true
});

#region Информация о старте приложения
// Initialize Tarantool connection (if enabled)
if (tarantoolEnabled)
{
    try
    {
        var tarantoolConnection = app.Services.GetRequiredService<SipIntegration.Tarantool.Abstractions.ITarantoolConnection>();
        await tarantoolConnection.ConnectAsync();
        Log.Logger.Information("✓ Connected to Tarantool");
    }
    catch (Exception ex)
    {
        Log.Logger.Warning("✗ Failed to connect to Tarantool: {Error}", ex.Message);
    }
}

Console.WriteLine("==================================================");
Console.WriteLine($"AuthService {appName} успешно запущен!");
Console.WriteLine($"Окружение: {environment}");
Console.WriteLine($"Версия: {version}");
Console.WriteLine($"URLs: {urls}");
Console.WriteLine($"Swagger UI: {urls.Split(';')[0]}/swagger");
Console.WriteLine($"Health: {urls.Split(';')[0]}/health");
Console.WriteLine($"Tarantool Cache Sandbox: {urls.Split(';')[0]}/tarantool-cache-sandbox.html");
Console.WriteLine($"База данных: {connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Database="))?.Replace("Database=", "")}");
Console.WriteLine($"Время запуска: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("==================================================");
#endregion


app.Run();
