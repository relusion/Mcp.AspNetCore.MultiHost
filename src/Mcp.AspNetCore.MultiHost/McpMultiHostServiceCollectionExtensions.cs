using Mcp.AspNetCore.MultiHost.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Extension methods for configuring MCP multi-host services.
/// </summary>
public static class McpMultiHostServiceCollectionExtensions
{
    /// <summary>
    /// Adds MCP multi-host services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureHosts">An action to configure the MCP hosts.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureHosts"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="McpMultiHostOptions"/> configured with the provided action
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IMcpHostRegistry"/> singleton for accessing registered hosts
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="McpHostDisposalService"/> for proper cleanup on shutdown
    ///   </description></item>
    /// </list>
    /// <para>
    /// Call <c>MapMcpMultiHost()</c> on the endpoint route builder to complete the setup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddMcpMultiHost(options =>
    /// {
    ///     options.AddHost("admin", host =>
    ///     {
    ///         host.WithRoutePrefix("/mcp/admin")
    ///             .ConfigureMcpServer(mcp => mcp
    ///                 .WithHttpTransport()
    ///                 .WithTools&lt;AdminTools&gt;());
    ///     });
    ///
    ///     options.AddHost("user", host =>
    ///     {
    ///         host.WithRoutePrefix("/mcp/user")
    ///             .ConfigureMcpServer(mcp => mcp
    ///                 .WithHttpTransport()
    ///                 .WithTools&lt;UserTools&gt;());
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMcpMultiHost(
        this IServiceCollection services,
        Action<McpMultiHostOptions> configureHosts)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureHosts);

        // Configure options
        services.Configure(configureHosts);

        // Register the host registry as singleton
        services.TryAddSingleton<IMcpHostRegistry, McpHostRegistry>();

        // Register the disposal service
        services.AddHostedService<McpHostDisposalService>();

        return services;
    }
}
