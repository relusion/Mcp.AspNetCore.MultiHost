using Microsoft.Extensions.Logging;

namespace Mcp.AspNetCore.MultiHost.Internal;

/// <summary>
/// Source-generated logging methods for MCP multi-host operations.
/// </summary>
/// <remarks>
/// Using <see cref="LoggerMessageAttribute"/> provides high-performance logging
/// with compile-time validation of message templates.
/// </remarks>
internal static partial class McpMultiHostLogEvents
{
    /// <summary>
    /// Logs when an MCP host is successfully mapped.
    /// </summary>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Mapped MCP host '{HostName}' at route prefix '{RoutePrefix}'")]
    public static partial void HostMapped(
        ILogger logger,
        string hostName,
        string routePrefix);

    /// <summary>
    /// Logs a warning when the discovery endpoint is enabled in Production.
    /// </summary>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Discovery endpoint enabled at '{Path}' in {Environment} environment - consider disabling in production")]
    public static partial void DiscoveryEndpointWarning(
        ILogger logger,
        string path,
        string environment);

    /// <summary>
    /// Logs an error when host container building fails.
    /// </summary>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Failed to build host '{HostName}' at '{RoutePrefix}': {Error}")]
    public static partial void HostBuildFailed(
        ILogger logger,
        string hostName,
        string routePrefix,
        string error);

    /// <summary>
    /// Logs when host containers are disposed.
    /// </summary>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Disposed {Count} MCP host container(s)")]
    public static partial void HostsDisposed(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs a warning when a host container fails to dispose.
    /// </summary>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Failed to dispose host '{HostName}': {Error}")]
    public static partial void HostDisposalFailed(
        ILogger logger,
        string hostName,
        string error);

    /// <summary>
    /// Logs when multi-host mapping starts.
    /// </summary>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Starting MCP multi-host mapping for {Count} host(s)")]
    public static partial void MappingStarted(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs when multi-host mapping completes.
    /// </summary>
    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Information,
        Message = "Completed MCP multi-host mapping: {Count} host(s) registered")]
    public static partial void MappingCompleted(
        ILogger logger,
        int count);
}
