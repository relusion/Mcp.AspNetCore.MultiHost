using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mcp.AspNetCore.MultiHost.Tests.Bridging;

public class McpServiceBridgeBuilderTests
{
    [Fact]
    public void ForwardSingleton_SharesExactInstance()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var singletonInstance = new TestService();
        rootServices.AddSingleton(singletonInstance);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardSingleton<TestService>();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedService = hostProvider.GetService<TestService>();

        // Assert
        resolvedService.Should().BeSameAs(singletonInstance);
    }

    [Fact]
    public void ForwardSingleton_ServiceNotRegistered_DoesNotRegister()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardSingleton<TestService>();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedService = hostProvider.GetService<TestService>();

        // Assert
        resolvedService.Should().BeNull();
    }

    [Fact]
    public void Forward_WithFactory_UsesFactoryResult()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var originalService = new TestService { Value = "original" };
        rootServices.AddSingleton(originalService);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.Forward<TestService>(sp =>
        {
            var original = sp.GetRequiredService<TestService>();
            return new TestService { Value = original.Value + "-modified" };
        });

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedService = hostProvider.GetService<TestService>();

        // Assert
        resolvedService.Should().NotBeNull();
        resolvedService!.Value.Should().Be("original-modified");
    }

    [Fact]
    public void Forward_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new McpServiceBridgeBuilder();

        // Act
        var act = () => builder.Forward<TestService>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForwardAspNetCoreDefaults_ForwardsLoggerFactory()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        rootServices.AddSingleton<ILoggerFactory>(loggerFactory);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardAspNetCoreDefaults();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedFactory = hostProvider.GetService<ILoggerFactory>();

        // Assert
        resolvedFactory.Should().BeSameAs(loggerFactory);
    }

    [Fact]
    public void ForwardAspNetCoreDefaults_ForwardsConfiguration()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Key"] = "Value" })
            .Build();
        rootServices.AddSingleton<IConfiguration>(configuration);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardAspNetCoreDefaults();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedConfig = hostProvider.GetService<IConfiguration>();

        // Assert
        resolvedConfig.Should().BeSameAs(configuration);
    }

    [Fact]
    public void ForwardAspNetCoreDefaults_CalledMultipleTimes_AppliesOnce()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        rootServices.AddSingleton<ILoggerFactory>(loggerFactory);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardAspNetCoreDefaults();
        builder.ForwardAspNetCoreDefaults();
        builder.ForwardAspNetCoreDefaults();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);

        // Assert - Should not throw duplicate registration exception
        var hostProvider = hostServices.BuildServiceProvider();
        var resolvedFactory = hostProvider.GetService<ILoggerFactory>();
        resolvedFactory.Should().BeSameAs(loggerFactory);
    }

    [Fact]
    public void HasCustomConfiguration_Initially_IsFalse()
    {
        // Arrange & Act
        var builder = new McpServiceBridgeBuilder();

        // Assert
        builder.HasCustomConfiguration.Should().BeFalse();
    }

    [Fact]
    public void HasCustomConfiguration_AfterForwardSingleton_IsTrue()
    {
        // Arrange
        var builder = new McpServiceBridgeBuilder();

        // Act
        builder.ForwardSingleton<TestService>();

        // Assert
        builder.HasCustomConfiguration.Should().BeTrue();
    }

    [Fact]
    public void HasCustomConfiguration_AfterForwardAspNetCoreDefaults_IsTrue()
    {
        // Arrange
        var builder = new McpServiceBridgeBuilder();

        // Act
        builder.ForwardAspNetCoreDefaults();

        // Assert
        builder.HasCustomConfiguration.Should().BeTrue();
    }

    [Fact]
    public void ChainedCalls_WorkCorrectly()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var service1 = new TestService { Value = "service1" };
        var service2 = new AnotherTestService { Name = "service2" };
        rootServices.AddSingleton(service1);
        rootServices.AddSingleton(service2);
        rootServices.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();

        // Act - chained calls
        builder
            .ForwardAspNetCoreDefaults()
            .ForwardSingleton<TestService>()
            .ForwardSingleton<AnotherTestService>();

        var hostServices = new ServiceCollection();
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();

        // Assert
        hostProvider.GetService<TestService>().Should().BeSameAs(service1);
        hostProvider.GetService<AnotherTestService>().Should().BeSameAs(service2);
        hostProvider.GetService<ILoggerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void ApplyTo_MultipleServices_AllForwarded()
    {
        // Arrange
        var rootServices = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        var config = new ConfigurationBuilder().Build();
        rootServices.AddSingleton<ILoggerFactory>(loggerFactory);
        rootServices.AddSingleton<IConfiguration>(config);
        var rootProvider = rootServices.BuildServiceProvider();

        var builder = new McpServiceBridgeBuilder();
        builder.ForwardAspNetCoreDefaults();

        var hostServices = new ServiceCollection();

        // Act
        builder.ApplyTo(hostServices, rootProvider);
        var hostProvider = hostServices.BuildServiceProvider();

        // Assert
        hostProvider.GetService<ILoggerFactory>().Should().BeSameAs(loggerFactory);
        hostProvider.GetService<IConfiguration>().Should().BeSameAs(config);
    }

    private class TestService
    {
        public string Value { get; set; } = "default";
    }

    private class AnotherTestService
    {
        public string Name { get; set; } = "default";
    }
}
