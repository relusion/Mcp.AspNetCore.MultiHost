using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Builder for configuring an individual MCP host within a multi-host application.
/// </summary>
/// <remarks>
/// Use this builder to configure the host's route prefix, MCP server settings,
/// endpoint conventions, and service bridging. The builder validates configuration
/// and creates a <see cref="McpHostDefinition"/> when <see cref="McpMultiHostOptions.AddHost"/> completes.
/// </remarks>
public sealed class McpHostBuilder
{
    private string? _routePrefix;
    private Action<IServiceCollection>? _configureHostServices;
    private Action<IMcpServerBuilder>? _configureMcpServer;
    private Action<IEndpointConventionBuilder>? _configureEndpoints;
    private Action<McpServiceBridgeBuilder>? _bridgeServices;

    /// <summary>
    /// Sets the route prefix for this MCP host.
    /// </summary>
    /// <param name="routePrefix">The route prefix, which must start with '/'.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="routePrefix"/> is null, whitespace, or doesn't start with '/'.</exception>
    /// <remarks>
    /// Trailing slashes are automatically removed for normalization (except for root "/").
    /// </remarks>
    /// <example>
    /// <code>
    /// host.WithRoutePrefix("/mcp/core");
    /// </code>
    /// </example>
    public McpHostBuilder WithRoutePrefix(string routePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        if (!routePrefix.StartsWith('/'))
        {
            throw new ArgumentException("Route prefix must start with '/'", nameof(routePrefix));
        }

        // Normalize: remove trailing slash (unless it's just "/")
        _routePrefix = routePrefix.Length > 1
            ? routePrefix.TrimEnd('/')
            : routePrefix;

        return this;
    }

    /// <summary>
    /// Configures additional services to be registered in this host's DI container.
    /// </summary>
    /// <param name="configure">Action to configure services.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// Services registered here are only available within this host's container.
    /// For shared services, use <see cref="BridgeServices"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// host.ConfigureHostServices(services =>
    /// {
    ///     services.AddSingleton&lt;IMyHostService, MyHostService&gt;();
    /// });
    /// </code>
    /// </example>
    public McpHostBuilder ConfigureHostServices(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureHostServices = configure;
        return this;
    }

    /// <summary>
    /// Configures the MCP server for this host.
    /// </summary>
    /// <param name="configure">Action to configure the MCP server.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// This is required for every host. Use the builder to register tools, resources,
    /// prompts, and configure server metadata. You must also call <c>WithHttpTransport()</c>
    /// within this action.
    /// </remarks>
    /// <example>
    /// <code>
    /// host.ConfigureMcpServer(mcp =>
    /// {
    ///     mcp.WithHttpTransport();
    ///     mcp.WithTools&lt;MyTools&gt;();
    /// });
    /// </code>
    /// </example>
    public McpHostBuilder ConfigureMcpServer(Action<IMcpServerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureMcpServer = configure;
        return this;
    }

    /// <summary>
    /// Configures endpoint conventions applied after the MCP endpoints are mapped.
    /// </summary>
    /// <param name="configure">Action to configure endpoint conventions.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// Use this to apply standard ASP.NET Core endpoint conventions such as
    /// authentication, rate limiting, CORS, and custom metadata.
    /// </remarks>
    /// <example>
    /// <code>
    /// host.ConfigureEndpoints(endpoints =>
    /// {
    ///     endpoints.RequireAuthorization("admin-policy");
    ///     endpoints.RequireRateLimiting("mcp-rate-limit");
    /// });
    /// </code>
    /// </example>
    public McpHostBuilder ConfigureEndpoints(Action<IEndpointConventionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureEndpoints = configure;
        return this;
    }

    /// <summary>
    /// Configures service bridging from the main application container.
    /// </summary>
    /// <param name="configure">Action to configure service bridging.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// If not called, <see cref="McpServiceBridgeBuilder.ForwardAspNetCoreDefaults"/> is applied automatically.
    /// Use this to forward additional services or customize bridging behavior.
    /// </remarks>
    /// <example>
    /// <code>
    /// host.BridgeServices(bridge =>
    /// {
    ///     bridge.ForwardAspNetCoreDefaults();
    ///     bridge.ForwardSingleton&lt;IMyCache&gt;();
    /// });
    /// </code>
    /// </example>
    public McpHostBuilder BridgeServices(Action<McpServiceBridgeBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _bridgeServices = configure;
        return this;
    }

    /// <summary>
    /// Builds the host definition from the current configuration.
    /// </summary>
    /// <param name="name">The name for this host.</param>
    /// <returns>A validated <see cref="McpHostDefinition"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    internal McpHostDefinition Build(string name)
    {
        if (string.IsNullOrWhiteSpace(_routePrefix))
        {
            throw new InvalidOperationException(
                $"Host '{name}' must have a route prefix. Call WithRoutePrefix().");
        }

        if (_configureMcpServer is null)
        {
            throw new InvalidOperationException(
                $"Host '{name}' must configure MCP server. Call ConfigureMcpServer().");
        }

        return new McpHostDefinition
        {
            Name = name,
            RoutePrefix = _routePrefix,
            ConfigureHostServices = _configureHostServices,
            ConfigureMcpServer = _configureMcpServer,
            ConfigureEndpoints = _configureEndpoints,
            BridgeServices = _bridgeServices
        };
    }
}
