namespace KoreForge.RestApi.Common.Persistence.Entities;

/// <summary>
/// Represents a single audited HTTP request when using split tables.
/// </summary>
public sealed partial class ApiCallRequestAudit : IResponseTimestamp
{
    public long Id { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset RequestTimestampUtc { get; set; }
    public DateTimeOffset ResponseTimestampUtc { get; set; }
    public long DurationMs { get; set; }
    public string? RequestPayload { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public string? CallerSystem { get; set; }
}