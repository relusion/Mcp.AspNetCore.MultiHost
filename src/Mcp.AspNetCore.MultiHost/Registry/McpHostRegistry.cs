using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Default implementation of <see cref="IMcpHostRegistry"/> providing thread-safe
/// storage and retrieval of registered MCP hosts.
/// </summary>
/// <remarks>
/// The registry maintains host information in a concurrent dictionary for thread-safe access.
/// Once sealed, the registry becomes read-only and no further hosts can be registered.
/// On disposal, all host containers are disposed asynchronously with error aggregation.
/// </remarks>
internal sealed class McpHostRegistry : IMcpHostRegistry
{
    private readonly ConcurrentDictionary<string, McpHostInfo> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<McpHostRegistry>? _logger;
    private volatile bool _isSealed;
    private volatile bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpHostRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public McpHostRegistry(ILogger<McpHostRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpHostInfo> Hosts => _hosts.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public McpHostInfo? TryGetHost(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _hosts.TryGetValue(name, out var host) ? host : null;
    }

    /// <inheritdoc />
    public bool IsSealed => _isSealed;

    /// <summary>
    /// Registers a new host in the registry.
    /// </summary>
    /// <param name="hostInfo">The host information to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostInfo"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the registry is sealed or a host with the same name is already registered.
    /// </exception>
    internal void Register(McpHostInfo hostInfo)
    {
        ArgumentNullException.ThrowIfNull(hostInfo);

        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Cannot register hosts after the registry has been sealed.");
        }

        if (!_hosts.TryAdd(hostInfo.Name, hostInfo))
        {
            throw new InvalidOperationException(
                $"Host '{hostInfo.Name}' is already registered in the registry.");
        }
    }

    /// <summary>
    /// Seals the registry, preventing further host registrations.
    /// </summary>
    /// <remarks>
    /// This method is called automatically at the end of <c>MapMcpMultiHost()</c>.
    /// Calling it multiple times has no additional effect.
    /// </remarks>
    internal void Seal()
    {
        _isSealed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        var exceptions = new List<Exception>();
        var disposedCount = 0;

        foreach (var (name, hostInfo) in _hosts)
        {
            try
            {
                if (hostInfo.ServiceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (hostInfo.ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                disposedCount++;
            }
            catch (Exception ex)
            {
                exceptions.Add(new InvalidOperationException(
                    $"Failed to dispose host '{name}': {ex.Message}", ex));
            }
        }

        _hosts.Clear();

        if (exceptions.Count == 0)
        {
            _logger?.LogInformation("Disposed {Count} MCP host container(s)", disposedCount);
        }
        else
        {
            foreach (var ex in exceptions)
            {
                _logger?.LogWarning(ex, "Host disposal failed: {Message}", ex.Message);
            }

            throw new AggregateException(
                $"Failed to dispose {exceptions.Count} host container(s)", exceptions);
        }
    }
}
