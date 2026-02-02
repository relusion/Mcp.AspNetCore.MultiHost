namespace Mcp.AspNetCore.MultiHost.Discovery;

/// <summary>
/// Summary information about a single MCP host.
/// </summary>
/// <param name="Name">The unique name of the host.</param>
/// <param name="RoutePrefix">The route prefix where the host is mapped.</param>
/// <remarks>
/// This record is used by the discovery endpoint to provide information
/// about available MCP hosts without exposing internal details.
/// </remarks>
public sealed record McpHostSummary(string Name, string RoutePrefix);
