namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Represents runtime information about a registered MCP host.
/// </summary>
/// <remarks>
/// This record is created during endpoint mapping and stored in the <see cref="IMcpHostRegistry"/>.
/// It provides access to the host's service provider for advanced scenarios, though direct access
/// to the service provider should be rare in typical usage.
/// </remarks>
/// <param name="Name">The unique name of this host.</param>
/// <param name="RoutePrefix">The route prefix where this host's endpoints are mapped.</param>
/// <param name="ServiceProvider">The host's DI container. Use with caution - prefer dependency injection.</param>
/// <param name="CreatedAt">The timestamp when this host's container was built.</param>
public record McpHostInfo(
    string Name,
    string RoutePrefix,
    IServiceProvider ServiceProvider,
    DateTimeOffset CreatedAt);
