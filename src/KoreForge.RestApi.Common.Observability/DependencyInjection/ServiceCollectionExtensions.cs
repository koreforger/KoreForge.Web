using KoreForge.RestApi.Common.Abstractions.Time;
using KoreForge.RestApi.Common.Observability.Clock;
using KoreForge.RestApi.Common.Observability.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KoreForge.RestApi.Common.Observability.DependencyInjection;

/// <summary>
/// Registers common observability primitives (clock + tracer).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonObservability(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IUtcClock, SystemUtcClock>();
        services.TryAddSingleton<ActivityTracer>();
        services.TryAddSingleton<ITracer>(sp => sp.GetRequiredService<ActivityTracer>());

        return services;
    }
}
