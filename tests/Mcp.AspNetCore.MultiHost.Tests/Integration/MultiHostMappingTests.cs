using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Integration;

public class MultiHostMappingTests : IAsyncLifetime
{
    private IHost? _host;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMcpMultiHost(options =>
                    {
                        options.AddHost("admin", host =>
                        {
                            host.WithRoutePrefix("/mcp/admin")
                                .ConfigureMcpServer(mcp => mcp.WithHttpTransport());
                        });

                        options.AddHost("user", host =>
                        {
                            host.WithRoutePrefix("/mcp/user")
                                .ConfigureMcpServer(mcp => mcp.WithHttpTransport());
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapMcpMultiHost(options =>
                        {
                            options.MapDiscoveryEndpoint = true;
                        });
                    });
                });
            })
            .Build();

        await _host.StartAsync();
        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Fact]
    public async Task MultipleHosts_MapToDistinctPrefixes()
    {
        // Act - Try to access both host endpoints
        var adminResponse = await _client!.GetAsync("/mcp/admin");
        var userResponse = await _client!.GetAsync("/mcp/user");

        // Assert - Both should not return 404 (they respond as MCP endpoints)
        // MCP endpoints return specific status codes, not 404
        adminResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        userResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DiscoveryEndpoint_ReturnsAllHosts()
    {
        // Act
        var response = await _client!.GetAsync("/mcp/_hosts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var hosts = json.RootElement.GetProperty("hosts");

        hosts.GetArrayLength().Should().Be(2);

        var hostNames = new List<string>();
        foreach (var host in hosts.EnumerateArray())
        {
            hostNames.Add(host.GetProperty("name").GetString()!);
        }

        hostNames.Should().Contain("admin");
        hostNames.Should().Contain("user");
    }

    [Fact]
    public async Task DiscoveryEndpoint_ReturnsCorrectRoutePrefixes()
    {
        // Act
        var response = await _client!.GetAsync("/mcp/_hosts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var hosts = json.RootElement.GetProperty("hosts");

        var routePrefixes = new Dictionary<string, string>();
        foreach (var host in hosts.EnumerateArray())
        {
            var name = host.GetProperty("name").GetString()!;
            var prefix = host.GetProperty("routePrefix").GetString()!;
            routePrefixes[name] = prefix;
        }

        routePrefixes["admin"].Should().Be("/mcp/admin");
        routePrefixes["user"].Should().Be("/mcp/user");
    }

    [Fact]
    public void Registry_IsSealed_AfterMapping()
    {
        // Arrange & Act
        var registry = _host!.Services.GetRequiredService<IMcpHostRegistry>();

        // Assert
        registry.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Registry_ContainsAllHosts()
    {
        // Arrange & Act
        var registry = _host!.Services.GetRequiredService<IMcpHostRegistry>();

        // Assert
        registry.Hosts.Should().HaveCount(2);
        registry.TryGetHost("admin").Should().NotBeNull();
        registry.TryGetHost("user").Should().NotBeNull();
    }
}
