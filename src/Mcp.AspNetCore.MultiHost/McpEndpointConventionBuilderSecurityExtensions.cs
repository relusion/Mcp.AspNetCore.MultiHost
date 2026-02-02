using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Mcp.AspNetCore.MultiHost;

/// <summary>
/// Security-related extension methods for MCP endpoint convention builders.
/// </summary>
public static class McpEndpointConventionBuilderSecurityExtensions
{
    /// <summary>
    /// Requires requests to have either no Origin header (non-browser) or an Origin
    /// that matches the request host or one of the explicitly allowed origins.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="allowedOrigins">Additional origins to allow (e.g., "https://trusted.example.com").</param>
    /// <returns>The endpoint convention builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This filter provides basic cross-origin protection for MCP endpoints:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Requests without an Origin header are allowed (non-browser clients)</description></item>
    ///   <item><description>Same-origin requests (Origin matches Host) are allowed</description></item>
    ///   <item><description>Requests from explicitly allowed origins are allowed</description></item>
    ///   <item><description>All other cross-origin requests return 403 Forbidden</description></item>
    /// </list>
    /// <para>
    /// This is NOT a replacement for CORS middleware but provides an additional layer
    /// of protection specifically for MCP endpoints.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddHost("admin", host =>
    /// {
    ///     host.WithRoutePrefix("/mcp/admin")
    ///         .ConfigureEndpoints(endpoints =>
    ///         {
    ///             endpoints.RequireSameOriginOrAllowed("https://admin.example.com");
    ///         });
    /// });
    /// </code>
    /// </example>
    public static TBuilder RequireSameOriginOrAllowed<TBuilder>(
        this TBuilder builder,
        params string[] allowedOrigins)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allowedOriginSet = new HashSet<string>(
            allowedOrigins ?? [],
            StringComparer.OrdinalIgnoreCase);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var origin = httpContext.Request.Headers.Origin.FirstOrDefault();

            // No Origin header - allow (non-browser clients)
            if (string.IsNullOrEmpty(origin))
            {
                return await next(context);
            }

            // Check if same-origin
            var requestScheme = httpContext.Request.Scheme;
            var requestHost = httpContext.Request.Host;
            var expectedOrigin = $"{requestScheme}://{requestHost}";

            if (string.Equals(origin, expectedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return await next(context);
            }

            // Check if explicitly allowed
            if (allowedOriginSet.Contains(origin))
            {
                return await next(context);
            }

            // Reject cross-origin request
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        });

        return builder;
    }
}
