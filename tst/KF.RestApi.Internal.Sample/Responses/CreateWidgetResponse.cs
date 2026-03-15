using KF.RestApi.Domain.Sample.Models;

namespace KF.RestApi.Internal.Sample.Responses;

/// <summary>
/// HTTP response envelope returned to clients.
/// </summary>
public sealed record CreateWidgetResponse(string Id, string Name, string? Status, string? CorrelationId)
{
    public static CreateWidgetResponse From(WidgetResult result)
        => new(result.Id, result.Name, result.Status, result.CorrelationId);
}
