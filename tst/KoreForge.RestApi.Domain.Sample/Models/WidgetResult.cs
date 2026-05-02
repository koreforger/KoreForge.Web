namespace KoreForge.RestApi.Domain.Sample.Models;

/// <summary>
/// Aggregated result emitted by the Domain layer.
/// </summary>
public sealed class WidgetResult
{
    public WidgetResult(string id, string name, string? status, string? correlationId)
    {
        Id = id;
        Name = name;
        Status = status;
        CorrelationId = correlationId;
    }

    public string Id { get; }

    public string Name { get; }

    public string? Status { get; }

    public string? CorrelationId { get; }
}
