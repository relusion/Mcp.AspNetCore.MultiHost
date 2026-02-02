namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Options for configuring how MCP hosts are mapped to endpoints.
/// </summary>
/// <remarks>
/// These options are applied during the <c>MapMcpMultiHost()</c> call and control
/// runtime behavior such as the optional discovery endpoint.
/// </remarks>
public sealed class McpMultiHostMapOptions
{
    /// <summary>
    /// Gets or sets whether to map a discovery endpoint that lists all registered hosts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c> for security reasons. When enabled, exposes an endpoint
    /// that returns the names and route prefixes of all registered hosts.
    /// </para>
    /// <para>
    /// <strong>Security Warning:</strong> Enabling this in production exposes host names
    /// and route prefixes, which may reveal internal architecture. A warning is logged
    /// when enabled in the Production environment.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapMcpMultiHost(options =>
    /// {
    ///     options.MapDiscoveryEndpoint = true;
    /// });
    /// </code>
    /// </example>
    public bool MapDiscoveryEndpoint { get; set; } = false;

    /// <summary>
    /// Gets or sets the path for the discovery endpoint.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="MapDiscoveryEndpoint"/> is <c>true</c>.
    /// Default is <c>/mcp/_hosts</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapMcpMultiHost(options =>
    /// {
    ///     options.MapDiscoveryEndpoint = true;
    ///     options.DiscoveryEndpointPath = "/api/mcp/hosts";
    /// });
    /// </code>
    /// </example>
    public string DiscoveryEndpointPath { get; set; } = "/mcp/_hosts";
}
