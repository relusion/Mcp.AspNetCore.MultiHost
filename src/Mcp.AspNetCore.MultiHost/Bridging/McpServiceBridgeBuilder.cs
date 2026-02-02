using Microsoft.Extensions.DependencyInjection;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Builder for configuring service bridging from the main application container to a host container.
/// </summary>
/// <remarks>
/// Service bridging allows host containers to access shared services from the main application,
/// such as logging, configuration, and custom application services.
/// </remarks>
public sealed class McpServiceBridgeBuilder
{
    private readonly List<Action<IServiceCollection, IServiceProvider>> _forwardingActions = [];
    private bool _aspNetCoreDefaultsApplied;

    /// <summary>
    /// Forwards a singleton service from the main application container to the host container.
    /// </summary>
    /// <typeparam name="TService">The service type to forward.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// The exact same instance from the main container will be registered in the host container.
    /// </remarks>
    public McpServiceBridgeBuilder ForwardSingleton<TService>() where TService : class
    {
        _forwardingActions.Add((services, rootProvider) =>
        {
            var instance = rootProvider.GetService<TService>();
            if (instance is not null)
            {
                services.AddSingleton(instance);
            }
        });
        return this;
    }

    /// <summary>
    /// Forwards a service using a factory function that resolves from the main application container.
    /// </summary>
    /// <typeparam name="TService">The service type to forward.</typeparam>
    /// <param name="factory">Factory function that receives the main container's service provider.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public McpServiceBridgeBuilder Forward<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        _forwardingActions.Add((services, rootProvider) =>
        {
            var instance = factory(rootProvider);
            if (instance is not null)
            {
                services.AddSingleton(instance);
            }
        });
        return this;
    }

    /// <summary>
    /// Forwards common ASP.NET Core services to the host container.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// This method forwards the following services if they are registered in the main container:
    /// <list type="bullet">
    ///   <item><description><c>ILoggerFactory</c> - For logging</description></item>
    ///   <item><description><c>IConfiguration</c> - For configuration access</description></item>
    ///   <item><description><c>IHostEnvironment</c> - For environment information</description></item>
    ///   <item><description><c>IHostApplicationLifetime</c> - For application lifetime events</description></item>
    ///   <item><description><c>IHttpContextAccessor</c> - For HTTP context access (if registered)</description></item>
    /// </list>
    /// This method is automatically called unless custom bridging is configured.
    /// </remarks>
    public McpServiceBridgeBuilder ForwardAspNetCoreDefaults()
    {
        if (_aspNetCoreDefaultsApplied)
        {
            return this;
        }

        _aspNetCoreDefaultsApplied = true;

        _forwardingActions.Add((services, rootProvider) =>
        {
            // ILoggerFactory
            var loggerFactory = rootProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            if (loggerFactory is not null)
            {
                services.AddSingleton(loggerFactory);

                // Also register ILogger<> so services can inject ILogger<T> directly
                // Logger<T> uses the forwarded ILoggerFactory
                services.Add(ServiceDescriptor.Singleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Logger<>)));
            }

            // IConfiguration
            var configuration = rootProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            if (configuration is not null)
            {
                services.AddSingleton(configuration);
            }

            // IHostEnvironment
            var hostEnvironment = rootProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            if (hostEnvironment is not null)
            {
                services.AddSingleton(hostEnvironment);
            }

            // IHostApplicationLifetime
            var hostLifetime = rootProvider.GetService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
            if (hostLifetime is not null)
            {
                services.AddSingleton(hostLifetime);
            }

            // IHttpContextAccessor (optional - only if registered)
            var httpContextAccessor = rootProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (httpContextAccessor is not null)
            {
                services.AddSingleton(httpContextAccessor);
            }
        });

        return this;
    }

    /// <summary>
    /// Applies the configured service forwarding to the target service collection.
    /// </summary>
    /// <param name="services">The host container's service collection.</param>
    /// <param name="rootProvider">The main application's service provider.</param>
    internal void ApplyTo(IServiceCollection services, IServiceProvider rootProvider)
    {
        foreach (var action in _forwardingActions)
        {
            action(services, rootProvider);
        }
    }

    /// <summary>
    /// Gets whether any forwarding has been configured.
    /// </summary>
    internal bool HasCustomConfiguration => _forwardingActions.Count > 0;
}
