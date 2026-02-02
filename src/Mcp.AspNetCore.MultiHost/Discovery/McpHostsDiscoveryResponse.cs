namespace Mcp.AspNetCore.MultiHost.Discovery;

/// <summary>
/// Response from the MCP hosts discovery endpoint.
/// </summary>
/// <param name="Hosts">The list of available MCP hosts.</param>
/// <remarks>
/// <para>
/// This response is returned by the discovery endpoint when enabled via
/// <see cref="McpMultiHostMapOptions.MapDiscoveryEndpoint"/>.
/// </para>
/// <para>
/// The response provides a list of all registered MCP hosts with their
/// names and route prefixes, allowing clients to discover available endpoints.
/// </para>
/// </remarks>
/// <example>
/// Example response:
/// <code>
/// {
///   "hosts": [
///     { "name": "admin", "routePrefix": "/mcp/admin" },
///     { "name": "user", "routePrefix": "/mcp/user" }
///   ]
/// }
/// </code>
/// </example>
public sealed record McpHostsDiscoveryResponse(IReadOnlyList<McpHostSummary> Hosts);
