using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;

namespace Mcp.AspNetCore.MultiHost.Internal;

/// <summary>
/// Factory for building isolated DI containers for MCP hosts.
/// </summary>
/// <remarks>
/// Each host gets its own service provider configured with:
/// <list type="number">
///   <item><description>Bridged services from the main application (logging, config, etc.)</description></item>
///   <item><description>Host-specific service registrations</description></item>
///   <item><description>MCP server services via <c>AddMcpServer()</c></description></item>
/// </list>
/// </remarks>
internal static class HostContainerFactory
{
    /// <summary>
    /// Builds a service provider for a specific MCP host.
    /// </summary>
    /// <param name="definition">The host definition containing configuration actions.</param>
    /// <param name="rootProvider">The main application's service provider for bridging.</param>
    /// <returns>A configured service provider for the host.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when container building fails, with host name and route prefix in the message.
    /// </exception>
    public static IServiceProvider BuildHostContainer(
        McpHostDefinition definition,
        IServiceProvider rootProvider)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(rootProvider);

        try
        {
            var services = new ServiceCollection();

            // Step 1: Apply service bridging
            ApplyServiceBridging(services, rootProvider, definition);

            // Step 2: Apply custom host services
            definition.ConfigureHostServices?.Invoke(services);

            // Step 3: Add MCP server and apply configuration
            var mcpBuilder = services.AddMcpServer();
            definition.ConfigureMcpServer(mcpBuilder);

            // Step 4: Build the service provider
            var environment = rootProvider.GetService<IHostEnvironment>();
            var validateScopes = environment?.IsDevelopment() ?? false;

            var options = new ServiceProviderOptions
            {
                ValidateScopes = validateScopes,
                ValidateOnBuild = validateScopes
            };

            return services.BuildServiceProvider(options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to build host '{definition.Name}' at '{definition.RoutePrefix}': {ex.Message}",
                ex);
        }
    }

    private static void ApplyServiceBridging(
        IServiceCollection services,
        IServiceProvider rootProvider,
        McpHostDefinition definition)
    {
        var bridgeBuilder = new McpServiceBridgeBuilder();

        if (definition.BridgeServices is not null)
        {
            // Custom bridging configuration
            definition.BridgeServices(bridgeBuilder);
        }
        else
        {
            // Default: forward ASP.NET Core defaults
            bridgeBuilder.ForwardAspNetCoreDefaults();
        }

        bridgeBuilder.ApplyTo(services, rootProvider);
    }
}
