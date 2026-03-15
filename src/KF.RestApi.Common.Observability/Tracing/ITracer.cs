namespace KF.RestApi.Common.Observability.Tracing;

/// <summary>
/// Contract for creating correlation-aware spans.
/// </summary>
public interface ITracer
{
    ITraceSpan StartSpan(string name);
}
