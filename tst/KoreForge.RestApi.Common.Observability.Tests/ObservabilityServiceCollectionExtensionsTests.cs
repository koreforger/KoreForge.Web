using KoreForge.RestApi.Common.Abstractions.Time;
using KoreForge.RestApi.Common.Observability.Clock;
using KoreForge.RestApi.Common.Observability.DependencyInjection;
using KoreForge.RestApi.Common.Observability.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KoreForge.RestApi.Common.Observability.Tests;

public sealed class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCommonObservability_RegistersClockAndTracer()
    {
        var services = new ServiceCollection();
        services.AddCommonObservability();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<SystemUtcClock>(provider.GetRequiredService<IUtcClock>());
        Assert.IsType<ActivityTracer>(provider.GetRequiredService<ITracer>());
    }

    [Fact]
    public void SystemUtcClock_ReturnsUtcTime()
    {
        var clock = new SystemUtcClock();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.InRange(now, before, after);
    }
}
