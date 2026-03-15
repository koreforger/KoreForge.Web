using KF.RestApi.Domain.Sample.Models;

namespace KF.RestApi.Domain.Sample.Services;

/// <summary>
/// Domain entry-point for orchestration logic.
/// </summary>
public interface ISampleService
{
    Task<WidgetResult> CreateWidgetAsync(CreateWidgetCommand command, CancellationToken cancellationToken = default);
}
