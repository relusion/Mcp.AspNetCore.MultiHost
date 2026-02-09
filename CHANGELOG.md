# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-preview] - 2026-02-02

### Added

- **Multi-Host Support**: Host multiple MCP servers in a single ASP.NET Core application
  - Each host runs at a distinct route prefix (e.g., `/mcp/admin`, `/mcp/user`)
  - Hosts have isolated DI containers with separate tool registrations

- **Fluent Configuration API**
  - `AddMcpMultiHost()` extension method for service registration
  - `MapMcpMultiHost()` extension method for endpoint mapping
  - Builder pattern for host configuration (`WithRoutePrefix`, `ConfigureMcpServer`, etc.)

- **Service Bridging**
  - `ForwardAspNetCoreDefaults()` bridges logging, configuration, and hosting services
  - `ForwardSingleton<T>()` bridges custom singleton services
  - `Forward<T>()` bridges services with custom factories

- **Discovery Endpoint**
  - Optional endpoint at `/mcp/_hosts` (configurable) lists all registered hosts
  - JSON response with host names and route prefixes

- **Security Features**
  - `RequireSameOriginOrAllowed()` endpoint filter for cross-origin protection
  - Integration with ASP.NET Core authorization via `ConfigureEndpoints`

- **Host Registry**
  - `IMcpHostRegistry` interface for runtime access to registered hosts
  - Thread-safe registration with automatic sealing after mapping

- **High-Performance Logging**
  - Source-generated logging using `[LoggerMessage]` attribute
  - Structured logging for host mapping, disposal, and errors

### Technical Details

- Targets .NET 8.0 and .NET 9.0
- Depends on ModelContextProtocol.AspNetCore 0.8.0-preview.1
- Uses proxy pattern for endpoint route builder integration
- Per-host DI containers with service provider validation in Development

### Known Limitations

- Discovery endpoint is intended for development/debugging; consider disabling in production
- Service bridging forwards instances, not registrations; scoped services require special handling
- MCP SDK version pinned; may require updates for future SDK releases
