namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Configuration options for registering multiple MCP hosts in an ASP.NET Core application.
/// </summary>
/// <remarks>
/// Use the <see cref="AddHost"/> method to register each MCP host with its own name,
/// route prefix, and configuration. Each host operates in isolation with its own DI container.
/// </remarks>
/// <example>
/// <code>
/// services.AddMcpMultiHost(options =>
/// {
///     options.AddHost("core", host =>
///     {
///         host.WithRoutePrefix("/mcp/core");
///         host.ConfigureMcpServer(mcp =>
///         {
///             mcp.WithHttpTransport();
///             mcp.WithTools&lt;CoreTools&gt;();
///         });
///     });
///
///     options.AddHost("admin", host =>
///     {
///         host.WithRoutePrefix("/mcp/admin");
///         host.ConfigureMcpServer(mcp =>
///         {
///             mcp.WithHttpTransport();
///             mcp.WithTools&lt;AdminTools&gt;();
///         });
///         host.ConfigureEndpoints(endpoints =>
///         {
///             endpoints.RequireAuthorization("admin-policy");
///         });
///     });
/// });
/// </code>
/// </example>
public sealed class McpMultiHostOptions
{
    private readonly List<McpHostDefinition> _hosts = [];
    private readonly HashSet<string> _hostNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _routePrefixes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the list of registered host definitions.
    /// </summary>
    /// <remarks>
    /// This collection is populated by calls to <see cref="AddHost"/>.
    /// Modifications should only be made through the <see cref="AddHost"/> method
    /// to ensure proper validation.
    /// </remarks>
    public IReadOnlyList<McpHostDefinition> Hosts => _hosts;

    /// <summary>
    /// Registers a new MCP host with the specified name and configuration.
    /// </summary>
    /// <param name="name">
    /// The unique name for this host. Names are compared case-insensitively for uniqueness.
    /// </param>
    /// <param name="configure">
    /// Action to configure the host using <see cref="McpHostBuilder"/>.
    /// </param>
    /// <returns>The created <see cref="McpHostDefinition"/> for further inspection if needed.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a host with the same name (case-insensitive) is already registered,
    /// when a host with the same route prefix is already registered,
    /// or when required configuration is missing from the builder.
    /// </exception>
    /// <example>
    /// <code>
    /// options.AddHost("myhost", host =>
    /// {
    ///     host.WithRoutePrefix("/mcp/myhost");
    ///     host.ConfigureMcpServer(mcp =>
    ///     {
    ///         mcp.WithHttpTransport();
    ///         mcp.WithTools&lt;MyTools&gt;();
    ///     });
    /// });
    /// </code>
    /// </example>
    public McpHostDefinition AddHost(string name, Action<McpHostBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        // Validate unique name (case-insensitive)
        if (!_hostNames.Add(name))
        {
            throw new InvalidOperationException($"Host '{name}' is already registered.");
        }

        var builder = new McpHostBuilder();
        configure(builder);

        McpHostDefinition definition;
        try
        {
            definition = builder.Build(name);

            // Validate unique route prefix (case-insensitive)
            if (!_routePrefixes.Add(definition.RoutePrefix))
            {
                throw new InvalidOperationException(
                    $"Route prefix '{definition.RoutePrefix}' is already registered by another host.");
            }
        }
        catch
        {
            // Remove the name from the set if building fails
            _hostNames.Remove(name);
            throw;
        }

        _hosts.Add(definition);
        return definition;
    }
}
