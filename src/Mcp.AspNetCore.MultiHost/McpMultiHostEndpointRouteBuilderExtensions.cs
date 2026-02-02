using Mcp.AspNetCore.MultiHost.Discovery;
using Mcp.AspNetCore.MultiHost.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Extension methods for mapping MCP multi-host endpoints.
/// </summary>
public static class McpMultiHostEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all configured MCP hosts to their respective route prefixes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddMcpMultiHost()</c> has not been called, or when host container building fails.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method must be called after <c>AddMcpMultiHost()</c> has been called during service registration.
    /// </para>
    /// <para>
    /// For each configured host, this method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Builds an isolated DI container with the host's services</description></item>
    ///   <item><description>Maps the MCP endpoint at the host's route prefix</description></item>
    ///   <item><description>Applies any configured endpoint conventions</description></item>
    ///   <item><description>Registers the host in the registry</description></item>
    /// </list>
    /// <para>
    /// After all hosts are mapped, the registry is sealed to prevent modification.
    /// </para>
    /// </remarks>
    public static IEndpointRouteBuilder MapMcpMultiHost(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMcpMultiHost(_ => { });
    }

    /// <summary>
    /// Maps all configured MCP hosts to their respective route prefixes with additional mapping options.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configureMapOptions">An action to configure mapping options.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="endpoints"/> or <paramref name="configureMapOptions"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddMcpMultiHost()</c> has not been called, or when host container building fails.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use the <paramref name="configureMapOptions"/> action to enable features like the discovery endpoint:
    /// </para>
    /// <code>
    /// app.MapMcpMultiHost(options =>
    /// {
    ///     options.MapDiscoveryEndpoint = true;
    ///     options.DiscoveryEndpointPath = "/mcp/_hosts";
    /// });
    /// </code>
    /// </remarks>
    public static IEndpointRouteBuilder MapMcpMultiHost(
        this IEndpointRouteBuilder endpoints,
        Action<McpMultiHostMapOptions> configureMapOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configureMapOptions);

        var rootProvider = endpoints.ServiceProvider;
        var options = rootProvider.GetRequiredService<IOptions<McpMultiHostOptions>>().Value;
        var registry = rootProvider.GetRequiredService<IMcpHostRegistry>() as McpHostRegistry
            ?? throw new InvalidOperationException(
                "IMcpHostRegistry is not the expected McpHostRegistry type. " +
                "Ensure AddMcpMultiHost() was called and the registry was not replaced.");

        var loggerFactory = rootProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("Mcp.AspNetCore.MultiHost");

        var mapOptions = new McpMultiHostMapOptions();
        configureMapOptions(mapOptions);

        if (logger is not null)
        {
            McpMultiHostLogEvents.MappingStarted(logger, options.Hosts.Count);
        }

        // Map each host
        foreach (var definition in options.Hosts)
        {
            MapSingleHost(endpoints, definition, rootProvider, registry, logger);
        }

        // Seal the registry to prevent further modifications
        registry.Seal();

        if (logger is not null)
        {
            McpMultiHostLogEvents.MappingCompleted(logger, options.Hosts.Count);
        }

        // Map discovery endpoint if enabled
        if (mapOptions.MapDiscoveryEndpoint)
        {
            MapDiscoveryEndpoint(endpoints, mapOptions, rootProvider, logger);
        }

        return endpoints;
    }

    private static void MapSingleHost(
        IEndpointRouteBuilder endpoints,
        McpHostDefinition definition,
        IServiceProvider rootProvider,
        McpHostRegistry registry,
        ILogger? logger)
    {
        try
        {
            // Build the host container
            var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

            // Create a proxy endpoint route builder that uses the host's service provider
            var hostRouteBuilder = new HostEndpointRouteBuilder(endpoints, hostProvider);

            // Map MCP at the host's route prefix
            var conventionBuilder = hostRouteBuilder.MapMcp(definition.RoutePrefix);

            // Apply any custom endpoint conventions
            definition.ConfigureEndpoints?.Invoke(conventionBuilder);

            // Register in the registry
            var hostInfo = new McpHostInfo(
                definition.Name,
                definition.RoutePrefix,
                hostProvider,
                DateTimeOffset.UtcNow);

            registry.Register(hostInfo);

            if (logger is not null)
            {
                McpMultiHostLogEvents.HostMapped(logger, definition.Name, definition.RoutePrefix);
            }
        }
        catch (Exception ex)
        {
            if (logger is not null)
            {
                McpMultiHostLogEvents.HostBuildFailed(logger, definition.Name, definition.RoutePrefix, ex.Message);
            }
            throw;
        }
    }

    private static void MapDiscoveryEndpoint(
        IEndpointRouteBuilder endpoints,
        McpMultiHostMapOptions mapOptions,
        IServiceProvider rootProvider,
        ILogger? logger)
    {
        var environment = rootProvider.GetService<IHostEnvironment>();
        var isProduction = environment?.IsProduction() ?? false;

        if (isProduction && logger is not null)
        {
            McpMultiHostLogEvents.DiscoveryEndpointWarning(
                logger,
                mapOptions.DiscoveryEndpointPath,
                environment?.EnvironmentName ?? "Unknown");
        }

        endpoints.MapGet(mapOptions.DiscoveryEndpointPath, (IMcpHostRegistry registry) =>
        {
            var hosts = registry.Hosts
                .Select(h => new McpHostSummary(h.Name, h.RoutePrefix))
                .ToList();

            return Results.Ok(new McpHostsDiscoveryResponse(hosts));
        })
        .WithName("McpMultiHostDiscovery")
        .WithTags("MCP")
        .Produces<McpHostsDiscoveryResponse>(StatusCodes.Status200OK);
    }
}
