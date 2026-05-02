namespace KoreForge.Web.HealthChecks;

/// <summary>
/// Well-known tag constants for health check endpoint filtering.
///
/// Use these tags when registering checks with <see cref="Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder"/>
/// so that <see cref="HealthCheckEndpointExtensions.MapKfHealthEndpoints"/> can route
/// each check to the correct endpoint.
///
/// <list type="table">
///   <listheader><term>Endpoint</term><description>Predicate</description></listheader>
///   <item><term>/health</term><description>all checks (full operational visibility)</description></item>
///   <item><term>/health/ready</term><description><see cref="Ready"/>-tagged checks (Kubernetes readiness probe)</description></item>
///   <item><term>/health/live</term><description><see cref="Live"/>-tagged checks (Kubernetes liveness probe)</description></item>
/// </list>
/// </summary>
public static class HealthTags
{
    /// <summary>Must pass for the application to be considered ready to serve traffic.</summary>
    public const string Ready = "ready";

    /// <summary>Must pass for the process to be considered alive (fast, no external I/O).</summary>
    public const string Live = "live";

    /// <summary>Checks related to SQL database connectivity or settings state.</summary>
    public const string Sql = "sql";

    /// <summary>Checks related to Kafka consumer or producer connectivity.</summary>
    public const string Kafka = "kafka";
}
