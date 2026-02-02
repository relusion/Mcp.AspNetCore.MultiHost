using FluentAssertions;
using Mcp.AspNetCore.MultiHost.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Internal;

public class HostContainerFactoryTests
{
    private static McpHostDefinition CreateDefinition(
        string name = "testhost",
        string routePrefix = "/mcp/test",
        Action<IServiceCollection>? configureHostServices = null,
        Action<McpServiceBridgeBuilder>? bridgeServices = null)
    {
        return new McpHostDefinition
        {
            Name = name,
            RoutePrefix = routePrefix,
            ConfigureHostServices = configureHostServices,
            ConfigureMcpServer = builder => builder.WithHttpTransport(),
            BridgeServices = bridgeServices
        };
    }

    [Fact]
    public void BuildHostContainer_NullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var rootProvider = new ServiceCollection().BuildServiceProvider();

        // Act
        var act = () => HostContainerFactory.BuildHostContainer(null!, rootProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildHostContainer_NullRootProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = CreateDefinition();

        // Act
        var act = () => HostContainerFactory.BuildHostContainer(definition, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildHostContainer_ValidDefinition_ReturnsServiceProvider()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var rootProvider = rootServices.BuildServiceProvider();

        var definition = CreateDefinition();

        // Act
        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        hostProvider.Should().NotBeNull();
        hostProvider.Should().NotBeSameAs(rootProvider);
    }

    [Fact]
    public void BuildHostContainer_AppliesDefaultBridging_WhenNotConfigured()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(loggerFactory);
        var rootProvider = rootServices.BuildServiceProvider();

        var definition = CreateDefinition(bridgeServices: null);

        // Act
        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        var resolvedFactory = hostProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        resolvedFactory.Should().BeSameAs(loggerFactory);
    }

    [Fact]
    public void BuildHostContainer_AppliesCustomBridging_WhenConfigured()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var customService = new CustomService { Value = "test" };
        rootServices.AddSingleton(customService);
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var rootProvider = rootServices.BuildServiceProvider();

        var definition = CreateDefinition(bridgeServices: bridge =>
        {
            bridge.ForwardAspNetCoreDefaults();
            bridge.ForwardSingleton<CustomService>();
        });

        // Act
        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        var resolvedService = hostProvider.GetService<CustomService>();
        resolvedService.Should().BeSameAs(customService);
    }

    [Fact]
    public void BuildHostContainer_AppliesConfigureHostServices()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var rootProvider = rootServices.BuildServiceProvider();

        var definition = CreateDefinition(
            configureHostServices: services =>
            {
                services.AddSingleton<HostSpecificService>();
            });

        // Act
        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        var hostService = hostProvider.GetService<HostSpecificService>();
        hostService.Should().NotBeNull();
    }

    [Fact]
    public void BuildHostContainer_HostServicesNotInRoot()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var rootProvider = rootServices.BuildServiceProvider();

        var definition = CreateDefinition(
            configureHostServices: services =>
            {
                services.AddSingleton<HostSpecificService>();
            });

        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Act
        var rootService = rootProvider.GetService<HostSpecificService>();
        var hostService = hostProvider.GetService<HostSpecificService>();

        // Assert
        rootService.Should().BeNull(); // Not in root
        hostService.Should().NotBeNull(); // Only in host
    }

    [Fact]
    public void BuildHostContainer_WrapsExceptionWithHostContext()
    {
        // Arrange
        var rootProvider = new ServiceCollection().BuildServiceProvider();

        var definition = new McpHostDefinition
        {
            Name = "badhost",
            RoutePrefix = "/mcp/bad",
            ConfigureMcpServer = _ => throw new InvalidOperationException("Test error")
        };

        // Act
        var act = () => HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to build host 'badhost' at '/mcp/bad'*Test error*");
    }

    [Fact]
    public void BuildHostContainer_InDevelopment_BuildsWithValidation()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);

        var lifetime = Substitute.For<IHostApplicationLifetime>();

        var rootServices = new ServiceCollection();
        rootServices.AddSingleton(environment);
        rootServices.AddSingleton(lifetime);
        rootServices.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        rootServices.AddLogging(); // Required for MCP SDK
        var rootProvider = rootServices.BuildServiceProvider();

        // A properly configured host should build even with ValidateOnBuild=true
        var definition = CreateDefinition(
            bridgeServices: bridge =>
            {
                bridge.ForwardAspNetCoreDefaults();
            });

        // Act - Should succeed in building (no validation errors means proper configuration)
        var hostProvider = HostContainerFactory.BuildHostContainer(definition, rootProvider);

        // Assert
        hostProvider.Should().NotBeNull();
    }

    private class CustomService
    {
        public string Value { get; set; } = "";
    }

    private class HostSpecificService { }

    private class ScopedService { }
}
