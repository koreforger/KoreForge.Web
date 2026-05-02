using KoreForge.RestApi.Common.Observability.Tracing;
using Xunit;

namespace KoreForge.RestApi.Common.Observability.Tests;

public sealed class ActivityTracerTests : IDisposable
{
    private readonly ActivityTracer _tracer = new();

    [Fact]
    public void StartSpan_ReturnsSpanWithCorrelationId()
    {
        using var span = _tracer.StartSpan("test-span");

        Assert.Equal("test-span", span.Name);
        Assert.False(string.IsNullOrWhiteSpace(span.CorrelationId));
    }

    [Fact]
    public void Dispose_PreventsFurtherSpans()
    {
        _tracer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _tracer.StartSpan("second"));
    }

    public void Dispose()
    {
        _tracer.Dispose();
        GC.SuppressFinalize(this);
    }
}
