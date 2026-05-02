using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace KoreForge.Web.HealthChecks;

/// <summary>
/// Extension methods on <see cref="IEndpointRouteBuilder"/> for mapping the standard
/// KoreForge health check endpoints.
///
/// These endpoints are the agreed convention across all KoreForge ASP.NET Core apps.
/// The actual health check implementations are registered per-library via
/// <see cref="Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder"/> extension methods.
/// </summary>
public static class HealthCheckEndpointExtensions
{
    /// <summary>
    /// Maps the standard KoreForge health endpoints:
    /// <list type="bullet">
    ///   <item>
    ///     <term><c>/health</c></term>
    ///     <description>All registered checks — full operational visibility.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/health/ready</c></term>
    ///     <description>
    ///       <see cref="HealthTags.Ready"/>-tagged checks only.
    ///       Used as the Kubernetes readiness probe — returns 200 when the app is ready
    ///       to receive traffic.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>/health/live</c></term>
    ///     <description>
    ///       <see cref="HealthTags.Live"/>-tagged checks only.
    ///       Used as the Kubernetes liveness probe — cheap, no external I/O.
    ///       Returns 200 as long as the process is functioning.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapKfHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(HealthTags.Ready)
        });

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(HealthTags.Live)
        });

        return endpoints;
    }
}
