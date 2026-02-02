using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Represents the configuration for a single MCP host within a multi-host application.
/// </summary>
/// <remarks>
/// Each host operates in isolation with its own DI container, tool set, and route prefix.
/// Host definitions are created through <see cref="McpHostBuilder"/> and validated at registration time.
/// </remarks>
public sealed record McpHostDefinition
{
    /// <summary>
    /// Gets the unique name identifying this host.
    /// </summary>
    /// <remarks>
    /// Names are compared case-insensitively for uniqueness validation.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the route prefix where this host's MCP endpoints will be mapped.
    /// </summary>
    /// <remarks>
    /// Must start with '/'. The MCP SDK will append its own endpoint paths (e.g., /sse, /message)
    /// to this prefix.
    /// </remarks>
    /// <example>
    /// A route prefix of "/mcp/core" will result in endpoints at "/mcp/core/sse", "/mcp/core/message", etc.
    /// </example>
    public required string RoutePrefix { get; init; }

    /// <summary>
    /// Gets the optional action to configure additional services in the host's DI container.
    /// </summary>
    /// <remarks>
    /// Use this to register host-specific services that are not part of MCP server configuration.
    /// These services will only be available within this host's container.
    /// </remarks>
    public Action<IServiceCollection>? ConfigureHostServices { get; init; }

    /// <summary>
    /// Gets the action to configure the MCP server for this host.
    /// </summary>
    /// <remarks>
    /// This is where tools, resources, prompts, and server metadata are configured.
    /// This action is required for every host definition.
    /// </remarks>
    public required Action<IMcpServerBuilder> ConfigureMcpServer { get; init; }

    /// <summary>
    /// Gets the optional action to configure endpoint conventions after mapping.
    /// </summary>
    /// <remarks>
    /// Use this to apply ASP.NET Core endpoint conventions such as:
    /// <list type="bullet">
    ///   <item><description><c>RequireAuthorization()</c> for authentication/authorization</description></item>
    ///   <item><description><c>RequireRateLimiting()</c> for rate limiting</description></item>
    ///   <item><description><c>RequireCors()</c> for CORS policies</description></item>
    ///   <item><description><c>WithMetadata()</c> for custom metadata</description></item>
    /// </list>
    /// </remarks>
    public Action<IEndpointConventionBuilder>? ConfigureEndpoints { get; init; }

    /// <summary>
    /// Gets the optional action to configure service bridging from the main application container.
    /// </summary>
    /// <remarks>
    /// By default, <see cref="McpServiceBridgeBuilder.ForwardAspNetCoreDefaults"/> is applied automatically,
    /// which forwards common services like ILoggerFactory, IConfiguration, and IHostEnvironment.
    /// Use this to forward additional application-specific services or to customize the default bridging.
    /// </remarks>
    public Action<McpServiceBridgeBuilder>? BridgeServices { get; init; }
}
