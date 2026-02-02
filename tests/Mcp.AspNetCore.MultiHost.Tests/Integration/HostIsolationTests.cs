using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Integration;

public class HostIsolationTests : IAsyncLifetime
{
    private IHost? _host;

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();

                    // Add shared service to root container
                    services.AddSingleton<SharedService>();

                    services.AddMcpMultiHost(options =>
                    {
                        options.AddHost("hostA", host =>
                        {
                            host.WithRoutePrefix("/mcp/a")
                                .ConfigureHostServices(hostServices =>
                                {
                                    // Host-specific service
                                    hostServices.AddSingleton<HostASpecificService>();
                                })
                                .BridgeServices(bridge =>
                                {
                                    bridge.ForwardAspNetCoreDefaults();
                                    bridge.ForwardSingleton<SharedService>();
                                })
                                .ConfigureMcpServer(mcp => mcp.WithHttpTransport());
                        });

                        options.AddHost("hostB", host =>
                        {
                            host.WithRoutePrefix("/mcp/b")
                                .ConfigureHostServices(hostServices =>
                                {
                                    // Different host-specific service
                                    hostServices.AddSingleton<HostBSpecificService>();
                                })
                                .BridgeServices(bridge =>
                                {
                                    bridge.ForwardAspNetCoreDefaults();
                                    bridge.ForwardSingleton<SharedService>();
                                })
                                .ConfigureMcpServer(mcp => mcp.WithHttpTransport());
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapMcpMultiHost();
                    });
                });
            })
            .Build();

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Fact]
    public void EachHost_HasSeparateServiceProvider()
    {
        // Arrange
        var registry = _host!.Services.GetRequiredService<IMcpHostRegistry>();

        // Act
        var hostA = registry.TryGetHost("hostA");
        var hostB = registry.TryGetHost("hostB");

        // Assert
        hostA.Should().NotBeNull();
        hostB.Should().NotBeNull();
        hostA!.ServiceProvider.Should().NotBeSameAs(hostB!.ServiceProvider);
    }

    [Fact]
    public void HostSpecificServices_NotSharedBetweenHosts()
    {
        // Arrange
        var registry = _host!.Services.GetRequiredService<IMcpHostRegistry>();

        var hostA = registry.TryGetHost("hostA");
        var hostB = registry.TryGetHost("hostB");

        // Act
        var serviceA = hostA!.ServiceProvider.GetService<HostASpecificService>();
        var serviceBInHostA = hostA!.ServiceProvider.GetService<HostBSpecificService>();

        var serviceB = hostB!.ServiceProvider.GetService<HostBSpecificService>();
        var serviceAInHostB = hostB!.ServiceProvider.GetService<HostASpecificService>();

        // Assert
        serviceA.Should().NotBeNull("HostA should have its specific service");
        serviceBInHostA.Should().BeNull("HostA should NOT have HostB's specific service");

        serviceB.Should().NotBeNull("HostB should have its specific service");
        serviceAInHostB.Should().BeNull("HostB should NOT have HostA's specific service");
    }

    [Fact]
    public void BridgedServices_AreSharedBetweenHosts()
    {
        // Arrange
        var rootProvider = _host!.Services;
        var registry = rootProvider.GetRequiredService<IMcpHostRegistry>();

        var sharedServiceFromRoot = rootProvider.GetService<SharedService>();
        var hostA = registry.TryGetHost("hostA");
        var hostB = registry.TryGetHost("hostB");

        // Act
        var sharedServiceFromHostA = hostA!.ServiceProvider.GetService<SharedService>();
        var sharedServiceFromHostB = hostB!.ServiceProvider.GetService<SharedService>();

        // Assert
        sharedServiceFromRoot.Should().NotBeNull();
        sharedServiceFromHostA.Should().NotBeNull();
        sharedServiceFromHostB.Should().NotBeNull();

        // All should be the same instance (bridged singleton)
        sharedServiceFromHostA.Should().BeSameAs(sharedServiceFromRoot);
        sharedServiceFromHostB.Should().BeSameAs(sharedServiceFromRoot);
    }
}

// Test services
public class SharedService
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class HostASpecificService
{
    public string Name => "HostA Service";
}

public class HostBSpecificService
{
    public string Name => "HostB Service";
}
