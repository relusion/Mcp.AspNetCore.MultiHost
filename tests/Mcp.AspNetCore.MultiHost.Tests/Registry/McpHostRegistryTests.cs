using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Registry;

public class McpHostRegistryTests
{
    private static McpHostInfo CreateHostInfo(string name, string routePrefix = "/mcp/test")
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        return new McpHostInfo(name, routePrefix, provider, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Hosts_InitiallyEmpty()
    {
        // Arrange & Act
        var registry = new McpHostRegistry();

        // Assert
        registry.Hosts.Should().BeEmpty();
    }

    [Fact]
    public void IsSealed_InitiallyFalse()
    {
        // Arrange & Act
        var registry = new McpHostRegistry();

        // Assert
        registry.IsSealed.Should().BeFalse();
    }

    [Fact]
    public void Register_AddsHostToRegistry()
    {
        // Arrange
        var registry = new McpHostRegistry();
        var hostInfo = CreateHostInfo("testhost");

        // Act
        registry.Register(hostInfo);

        // Assert
        registry.Hosts.Should().HaveCount(1);
        registry.Hosts[0].Should().BeSameAs(hostInfo);
    }

    [Fact]
    public void Register_NullHostInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_AfterSealed_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new McpHostRegistry();
        registry.Seal();
        var hostInfo = CreateHostInfo("testhost");

        // Act
        var act = () => registry.Register(hostInfo);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*registry has been sealed*");
    }

    [Fact]
    public void Register_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new McpHostRegistry();
        registry.Register(CreateHostInfo("testhost"));

        // Act
        var act = () => registry.Register(CreateHostInfo("testhost"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'testhost' is already registered*");
    }

    [Fact]
    public void Register_DuplicateNameCaseInsensitive_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new McpHostRegistry();
        registry.Register(CreateHostInfo("TestHost"));

        // Act
        var act = () => registry.Register(CreateHostInfo("TESTHOST"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void TryGetHost_ExistingHost_ReturnsHost()
    {
        // Arrange
        var registry = new McpHostRegistry();
        var hostInfo = CreateHostInfo("testhost");
        registry.Register(hostInfo);

        // Act
        var result = registry.TryGetHost("testhost");

        // Assert
        result.Should().BeSameAs(hostInfo);
    }

    [Fact]
    public void TryGetHost_CaseInsensitive_ReturnsHost()
    {
        // Arrange
        var registry = new McpHostRegistry();
        var hostInfo = CreateHostInfo("TestHost");
        registry.Register(hostInfo);

        // Act
        var result = registry.TryGetHost("TESTHOST");

        // Assert
        result.Should().BeSameAs(hostInfo);
    }

    [Fact]
    public void TryGetHost_NonExistingHost_ReturnsNull()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        var result = registry.TryGetHost("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetHost_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        var act = () => registry.TryGetHost(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Seal_SetsIsSealedToTrue()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        registry.Seal();

        // Assert
        registry.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Seal_MultipleCalls_NoEffect()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        registry.Seal();
        registry.Seal();
        registry.Seal();

        // Assert
        registry.IsSealed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllContainers()
    {
        // Arrange
        var registry = new McpHostRegistry();
        var services = new ServiceCollection();
        // Register as factory so DI owns the lifetime
        services.AddSingleton<DisposableService>();
        var provider = services.BuildServiceProvider();

        // Resolve to create the instance (DI will dispose factory-created instances)
        var resolvedService = provider.GetRequiredService<DisposableService>();

        var hostInfo = new McpHostInfo("testhost", "/mcp/test", provider, DateTimeOffset.UtcNow);
        registry.Register(hostInfo);

        // Act
        await registry.DisposeAsync();

        // Assert
        resolvedService.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_ClearsHostsList()
    {
        // Arrange
        var registry = new McpHostRegistry();
        registry.Register(CreateHostInfo("host1"));
        registry.Register(CreateHostInfo("host2"));

        // Act
        await registry.DisposeAsync();

        // Assert
        registry.Hosts.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_NoEffect()
    {
        // Arrange
        var registry = new McpHostRegistry();
        registry.Register(CreateHostInfo("testhost"));

        // Act
        await registry.DisposeAsync();
        await registry.DisposeAsync(); // Second call should not throw

        // Assert
        registry.Hosts.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_ContinuesOnError_ThrowsAggregateException()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Create a provider that throws on dispose
        var throwingProvider = new ThrowingServiceProvider();
        var hostInfo1 = new McpHostInfo("throwing", "/mcp/throwing", throwingProvider, DateTimeOffset.UtcNow);

        // Create a normal provider
        var normalProvider = new ServiceCollection().BuildServiceProvider();
        var hostInfo2 = new McpHostInfo("normal", "/mcp/normal", normalProvider, DateTimeOffset.UtcNow);

        registry.Register(hostInfo1);
        registry.Register(hostInfo2);

        // Act
        var act = async () => await registry.DisposeAsync();

        // Assert
        await act.Should().ThrowAsync<AggregateException>()
            .Where(ex => ex.InnerExceptions.Count == 1);
    }

    [Fact]
    public void Register_MultipleHosts_AllAccessible()
    {
        // Arrange
        var registry = new McpHostRegistry();

        // Act
        registry.Register(CreateHostInfo("core", "/mcp/core"));
        registry.Register(CreateHostInfo("admin", "/mcp/admin"));
        registry.Register(CreateHostInfo("tenant", "/mcp/tenant"));

        // Assert
        registry.Hosts.Should().HaveCount(3);
        registry.TryGetHost("core").Should().NotBeNull();
        registry.TryGetHost("admin").Should().NotBeNull();
        registry.TryGetHost("tenant").Should().NotBeNull();
    }

    private class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private class ThrowingServiceProvider : IServiceProvider, IDisposable
    {
        public object? GetService(Type serviceType) => null;
        public void Dispose() => throw new InvalidOperationException("Intentional test exception");
    }
}
