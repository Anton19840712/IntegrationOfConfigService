using System.Net.Http.Json;

namespace API.Services.Eureka;

/// <summary>
/// Фоновый сервис для регистрации в Eureka с retry логикой
/// </summary>
public class EurekaRegistrationService : IHostedService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EurekaRegistrationService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private Timer? _heartbeatTimer;
    private string? _instanceId;
    private string? _serviceName;
    private string? _eurekaUrl;

    public EurekaRegistrationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EurekaRegistrationService> logger,
        IHostApplicationLifetime lifetime)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var autoRegister = _configuration.GetValue<bool>("Eureka:AutoRegister", true);
        if (!autoRegister)
        {
            _logger.LogInformation("[Eureka] Автоматическая регистрация отключена");
            return Task.CompletedTask;
        }

        // Регистрируемся на событие ApplicationStarted
        _lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("[Eureka] Приложение запущено, начинаем регистрацию");

                // Дополнительная задержка для стабилизации
                await Task.Delay(TimeSpan.FromSeconds(2));

                // Попытки регистрации с retry
                const int maxRetries = 5;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    _logger.LogInformation("[Eureka] Попытка регистрации {Attempt}/{MaxRetries}", attempt, maxRetries);

                    var success = await RegisterAsync();

                    if (success)
                    {
                        _logger.LogInformation("✓ [Eureka] Registration successful on attempt {Attempt}", attempt);
                        StartHeartbeat();
                        return;
                    }

                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s, 16s, 32s
                        _logger.LogWarning("[Eureka] Попытка {Attempt} не удалась, ожидание {Delay}s",
                            attempt, delay.TotalSeconds);
                        await Task.Delay(delay);
                    }
                }

                _logger.LogError("✗ [Eureka] Failed to register after {MaxRetries} attempts", maxRetries);
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _heartbeatTimer?.Dispose();

        var autoDeregister = _configuration.GetValue<bool>("Eureka:AutoDeregister", true);
        if (autoDeregister && !string.IsNullOrEmpty(_instanceId))
        {
            _ = DeregisterAsync();
        }

        return Task.CompletedTask;
    }

    private async Task<bool> RegisterAsync()
    {
        try
        {
            _serviceName = _configuration["ServiceDiscovery:ServiceName"] ?? "auth-service";
            var serviceAddress = _configuration["ServiceDiscovery:ServiceAddress"] ?? "localhost";
            var servicePort = _configuration.GetValue<int>("ServiceDiscovery:ServicePort", 5026);
            _eurekaUrl = _configuration["Eureka:ServerUrl"] ?? "http://localhost:8761/eureka";

            _instanceId = $"{serviceAddress}:{_serviceName}:{servicePort}";

            var instance = new EurekaInstance
            {
                InstanceId = _instanceId,
                HostName = serviceAddress,
                App = _serviceName.ToUpperInvariant(),
                IpAddr = serviceAddress,
                Status = "UP",
                Port = new PortInfo { Value = servicePort, Enabled = "true" },
                SecurePort = new PortInfo { Value = 443, Enabled = "false" },
                HealthCheckUrl = $"http://{serviceAddress}:{servicePort}/health",
                StatusPageUrl = $"http://{serviceAddress}:{servicePort}",
                HomePageUrl = $"http://{serviceAddress}:{servicePort}",
                VipAddress = _serviceName,
                SecureVipAddress = _serviceName,
                DataCenterInfo = new DataCenterInfo
                {
                    Class = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                    Name = "MyOwn"
                }
            };

            var request = new EurekaRegistrationRequest { Instance = instance };
            var url = $"{_eurekaUrl}/apps/{_serviceName.ToUpperInvariant()}";

            _logger.LogDebug("[Eureka] POST {Url}", url);
            _logger.LogDebug("[Eureka] InstanceId: {InstanceId}", _instanceId);

            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ [Eureka] Service registered ({Address}:{Port})",
                    serviceAddress, servicePort);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠ [Eureka] Registration failed (HTTP {StatusCode}): {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ [Eureka] Exception during registration");
            return false;
        }
    }

    private void StartHeartbeat()
    {
        var heartbeatInterval = TimeSpan.FromSeconds(
            _configuration.GetValue<int>("Eureka:HeartbeatIntervalSeconds", 30));

        _heartbeatTimer = new Timer(
            async _ => await SendHeartbeatAsync(),
            null,
            heartbeatInterval,
            heartbeatInterval
        );

        _logger.LogInformation("[Eureka] Heartbeat запущен с интервалом {Interval}s", heartbeatInterval.TotalSeconds);
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_instanceId) || string.IsNullOrEmpty(_serviceName) || string.IsNullOrEmpty(_eurekaUrl))
                return;

            var url = $"{_eurekaUrl}/apps/{_serviceName.ToUpperInvariant()}/{_instanceId}";
            var response = await _httpClient.PutAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[Eureka] Heartbeat отправлен успешно");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[Eureka] Heartbeat вернул 404 - повторная регистрация");
                await RegisterAsync();
            }
            else
            {
                _logger.LogWarning("[Eureka] Heartbeat failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Eureka] Ошибка при отправке heartbeat");
        }
    }

    private async Task DeregisterAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_instanceId) || string.IsNullOrEmpty(_serviceName) || string.IsNullOrEmpty(_eurekaUrl))
                return;

            var url = $"{_eurekaUrl}/apps/{_serviceName.ToUpperInvariant()}/{_instanceId}";
            var response = await _httpClient.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Eureka] Сервис успешно отменил регистрацию");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Eureka] Ошибка при отмене регистрации");
        }
    }
}
