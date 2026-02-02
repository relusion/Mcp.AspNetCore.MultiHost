using Microsoft.Extensions.Hosting;

namespace Mcp.AspNetCore.MultiHost.Internal;

/// <summary>
/// Background service that ensures proper disposal of MCP host containers on application shutdown.
/// </summary>
/// <remarks>
/// <para>
/// This service implements <see cref="IHostedService"/> to hook into the ASP.NET Core host lifecycle.
/// During startup (<see cref="StartAsync"/>), no action is taken. During shutdown (<see cref="StopAsync"/>),
/// the service disposes all registered host containers via the <see cref="IMcpHostRegistry"/>.
/// </para>
/// <para>
/// The service is registered automatically by <c>AddMcpMultiHost()</c> and does not require
/// manual configuration.
/// </para>
/// </remarks>
internal sealed class McpHostDisposalService : IHostedService
{
    private readonly IMcpHostRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpHostDisposalService"/> class.
    /// </summary>
    /// <param name="registry">The host registry containing all registered MCP hosts.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is null.</exception>
    public McpHostDisposalService(IMcpHostRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Called when the application starts. No action is taken.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // No-op: Host containers are built during MapMcpMultiHost()
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the application shuts down. Disposes all host containers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the disposal operation.</returns>
    /// <remarks>
    /// Disposal continues even if individual host containers fail to dispose.
    /// Errors are aggregated and logged by the registry.
    /// </remarks>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _registry.DisposeAsync().ConfigureAwait(false);
    }
}
