namespace KoreForge.RestApi.Client.Sample.Models;

/// <summary>
/// Result returned to first-party consumers.
/// </summary>
public sealed record CreateWidgetResult(string Id, string Name, string? Status, string? CorrelationId);
