namespace KoreForge.RestApi.Common.Observability.Tracing;

/// <summary>
/// Represents an in-flight diagnostic span that carries correlation metadata.
/// </summary>
public interface ITraceSpan : IDisposable
{
    string Name { get; }
    string CorrelationId { get; }
}
