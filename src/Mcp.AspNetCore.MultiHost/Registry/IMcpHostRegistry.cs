namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Registry providing access to registered MCP host information.
/// </summary>
/// <remarks>
/// <para>
/// The registry is populated during <c>MapMcpMultiHost()</c> and becomes sealed (read-only)
/// after mapping completes. This ensures host information is stable during application runtime.
/// </para>
/// <para>
/// The registry is registered as a singleton in the main application container and can be
/// injected into services that need to discover available hosts.
/// </para>
/// </remarks>
public interface IMcpHostRegistry : IAsyncDisposable
{
    /// <summary>
    /// Gets the list of all registered hosts.
    /// </summary>
    /// <remarks>
    /// This collection is populated during endpoint mapping and becomes fixed once the registry is sealed.
    /// </remarks>
    IReadOnlyList<McpHostInfo> Hosts { get; }

    /// <summary>
    /// Attempts to retrieve host information by name.
    /// </summary>
    /// <param name="name">The host name to look up (case-insensitive).</param>
    /// <returns>The host information if found; otherwise, <c>null</c>.</returns>
    McpHostInfo? TryGetHost(string name);

    /// <summary>
    /// Gets whether the registry has been sealed.
    /// </summary>
    /// <remarks>
    /// Once sealed, no more hosts can be registered. The registry is sealed automatically
    /// at the end of <c>MapMcpMultiHost()</c>.
    /// </remarks>
    bool IsSealed { get; }
}
