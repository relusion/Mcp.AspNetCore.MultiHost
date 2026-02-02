using FluentAssertions;
using Mcp.AspNetCore.MultiHost.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Internal;

public class HostEndpointRouteBuilderTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Arrange
        var hostProvider = new ServiceCollection().BuildServiceProvider();

        // Act
        var act = () => new HostEndpointRouteBuilder(null!, hostProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullHostProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<IEndpointRouteBuilder>();

        // Act
        var act = () => new HostEndpointRouteBuilder(inner, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("hostProvider");
    }

    [Fact]
    public void ServiceProvider_ReturnsHostProvider_NotInnerProvider()
    {
        // Arrange
        var innerProvider = new ServiceCollection().BuildServiceProvider();
        var hostProvider = new ServiceCollection().BuildServiceProvider();

        var inner = Substitute.For<IEndpointRouteBuilder>();
        inner.ServiceProvider.Returns(innerProvider);

        var proxy = new HostEndpointRouteBuilder(inner, hostProvider);

        // Act
        var result = proxy.ServiceProvider;

        // Assert
        result.Should().BeSameAs(hostProvider);
        result.Should().NotBeSameAs(innerProvider);
    }

    [Fact]
    public void DataSources_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IEndpointRouteBuilder>();
        var dataSources = new List<EndpointDataSource>();
        inner.DataSources.Returns(dataSources);

        var hostProvider = new ServiceCollection().BuildServiceProvider();
        var proxy = new HostEndpointRouteBuilder(inner, hostProvider);

        // Act
        var result = proxy.DataSources;

        // Assert
        result.Should().BeSameAs(dataSources);
    }

    [Fact]
    public void CreateApplicationBuilder_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IEndpointRouteBuilder>();
        var expectedBuilder = Substitute.For<IApplicationBuilder>();
        inner.CreateApplicationBuilder().Returns(expectedBuilder);

        var hostProvider = new ServiceCollection().BuildServiceProvider();
        var proxy = new HostEndpointRouteBuilder(inner, hostProvider);

        // Act
        var result = proxy.CreateApplicationBuilder();

        // Assert
        result.Should().BeSameAs(expectedBuilder);
        inner.Received(1).CreateApplicationBuilder();
    }

    [Fact]
    public void ServiceProvider_ResolvesFromHostContainer()
    {
        // Arrange
        var inner = Substitute.For<IEndpointRouteBuilder>();
        var innerProvider = new ServiceCollection()
            .AddSingleton(new MarkerService("inner"))
            .BuildServiceProvider();
        inner.ServiceProvider.Returns(innerProvider);

        var hostProvider = new ServiceCollection()
            .AddSingleton(new MarkerService("host"))
            .BuildServiceProvider();

        var proxy = new HostEndpointRouteBuilder(inner, hostProvider);

        // Act
        var service = proxy.ServiceProvider.GetService<MarkerService>();

        // Assert
        service.Should().NotBeNull();
        service!.Name.Should().Be("host");
    }

    private class MarkerService
    {
        public string Name { get; }
        public MarkerService(string name) => Name = name;
    }
}
