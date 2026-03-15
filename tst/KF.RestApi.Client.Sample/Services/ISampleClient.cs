using KF.RestApi.Client.Sample.Models;

namespace KF.RestApi.Client.Sample.Services;

/// <summary>
/// Contract exposed to downstream callers.
/// </summary>
public interface ISampleClient
{
    Task<CreateWidgetResult> CreateWidgetAsync(CreateWidgetRequest request, CancellationToken cancellationToken = default);
}
