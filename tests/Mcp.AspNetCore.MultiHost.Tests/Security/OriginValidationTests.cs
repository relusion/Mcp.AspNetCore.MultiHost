using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Security;

public class OriginValidationTests : IAsyncLifetime
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
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Endpoint without origin validation
                        endpoints.MapGet("/open", () => Results.Ok("open"))
                            .WithName("Open");

                        // Endpoint with origin validation, no allowed origins
                        endpoints.MapGet("/protected", () => Results.Ok("protected"))
                            .RequireSameOriginOrAllowed()
                            .WithName("Protected");

                        // Endpoint with specific allowed origins
                        endpoints.MapGet("/allowed-origins", () => Results.Ok("allowed"))
                            .RequireSameOriginOrAllowed("https://trusted.example.com", "https://another-trusted.com")
                            .WithName("AllowedOrigins");
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
    public async Task NoOriginHeader_AllowsRequest()
    {
        // Act - Request without Origin header (non-browser client)
        var response = await _client!.GetAsync("/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SameOrigin_AllowsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Origin", "http://localhost");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DifferentOrigin_BlocksRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Origin", "https://attacker.com");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllowedOrigin_AllowsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/allowed-origins");
        request.Headers.Add("Origin", "https://trusted.example.com");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllowedOrigin_CaseInsensitive_AllowsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/allowed-origins");
        request.Headers.Add("Origin", "HTTPS://TRUSTED.EXAMPLE.COM");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotAllowedOrigin_BlocksRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/allowed-origins");
        request.Headers.Add("Origin", "https://untrusted.com");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OpenEndpoint_AllowsAnyOrigin()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/open");
        request.Headers.Add("Origin", "https://any-origin.com");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleAllowedOrigins_SecondOrigin_Allowed()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/allowed-origins");
        request.Headers.Add("Origin", "https://another-trusted.com");

        // Act
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
