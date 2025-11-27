using SipIntegration.Tarantool.Abstractions;
using ProGaudi.Tarantool.Client;

namespace Infrastructure.Services;

/// <summary>
/// No-op реализация ITarantoolConnection для случаев когда Tarantool отключен
/// </summary>
public class NoOpTarantoolConnection : ITarantoolConnection
{
    public bool IsConnected => false;

    public IBox GetClient()
    {
        throw new InvalidOperationException("Tarantool is disabled");
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(Func<IBox, Task> action, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Tarantool is disabled");
    }

    public Task<T> ExecuteAsync<T>(Func<IBox, Task<T>> action, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Tarantool is disabled");
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
