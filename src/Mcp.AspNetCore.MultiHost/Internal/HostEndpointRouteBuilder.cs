using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Mcp.AspNetCore.MultiHost.Internal;

/// <summary>
/// A proxy implementation of <see cref="IEndpointRouteBuilder"/> that substitutes the
/// service provider with a host-specific provider while delegating all other operations
/// to the inner builder.
/// </summary>
/// <remarks>
/// <para>
/// This proxy is the key integration point with the MCP SDK. When <c>MapMcp()</c> is called
/// on this proxy, it resolves <c>StreamableHttpHandler</c> from the host-specific provider
/// rather than the main application provider, enabling isolated MCP servers per host.
/// </para>
/// <para>
/// The proxy is only used at mapping time - it does not appear in the request pipeline.
/// All endpoint data sources are delegated to the inner builder, ensuring proper
/// integration with ASP.NET Core's routing infrastructure.
/// </para>
/// </remarks>
internal sealed class HostEndpointRouteBuilder : IEndpointRouteBuilder
{
    private readonly IEndpointRouteBuilder _inner;
    private readonly IServiceProvider _hostProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostEndpointRouteBuilder"/> class.
    /// </summary>
    /// <param name="inner">The real endpoint route builder to delegate to.</param>
    /// <param name="hostProvider">The host-specific service provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> or <paramref name="hostProvider"/> is null.
    /// </exception>
    public HostEndpointRouteBuilder(IEndpointRouteBuilder inner, IServiceProvider hostProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
    }

    /// <summary>
    /// Gets the host-specific service provider.
    /// </summary>
    /// <remarks>
    /// This is the key override - when <c>MapMcp()</c> resolves services,
    /// it will use the host's isolated container instead of the main application container.
    /// </remarks>
    public IServiceProvider ServiceProvider => _hostProvider;

    /// <summary>
    /// Gets the endpoint data sources, delegated to the inner builder.
    /// </summary>
    /// <remarks>
    /// Endpoints mapped through this proxy are added to the inner builder's data sources,
    /// ensuring they are registered with the actual ASP.NET Core routing system.
    /// </remarks>
    public ICollection<EndpointDataSource> DataSources => _inner.DataSources;

    /// <summary>
    /// Creates an application builder, delegated to the inner builder.
    /// </summary>
    /// <returns>A new <see cref="IApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// The application builder is used for endpoint-specific middleware configuration.
    /// Delegation ensures consistent behavior with the main application pipeline.
    /// </remarks>
    public IApplicationBuilder CreateApplicationBuilder() => _inner.CreateApplicationBuilder();
}
