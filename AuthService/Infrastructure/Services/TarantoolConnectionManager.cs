using SipIntegration.Tarantool.Abstractions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Управляет состоянием подключения к Tarantool с возможностью динамического включения/отключения
/// </summary>
public class TarantoolConnectionManager
{
    private readonly ITarantoolConnection _connection;
    private readonly ILogger<TarantoolConnectionManager> _logger;
    private bool _isEnabled;

    public TarantoolConnectionManager(
        ITarantoolConnection connection,
        ILogger<TarantoolConnectionManager> logger,
        bool initiallyEnabled)
    {
        _connection = connection;
        _logger = logger;
        _isEnabled = initiallyEnabled;
    }

    public bool IsEnabled => _isEnabled;

    public bool IsConnected => _isEnabled && _connection.IsConnected;

    public ITarantoolConnection Connection => _connection;

    public void Enable()
    {
        if (_isEnabled)
        {
            _logger.LogInformation("Tarantool already enabled");
            return;
        }

        _logger.LogInformation("Enabling Tarantool...");
        _isEnabled = true;

        // Try to connect if not already connected
        if (!_connection.IsConnected)
        {
            try
            {
                _connection.ConnectAsync().Wait();
                _logger.LogInformation("✓ Tarantool enabled and connected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to connect to Tarantool after enabling");
            }
        }
    }

    public void Disable()
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Tarantool already disabled");
            return;
        }

        _logger.LogInformation("Disabling Tarantool...");
        _isEnabled = false;
        _logger.LogInformation("✓ Tarantool disabled (connection kept alive but not used)");
    }
}
