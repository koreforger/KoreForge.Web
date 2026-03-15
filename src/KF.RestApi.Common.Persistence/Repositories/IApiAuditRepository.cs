using KF.RestApi.Common.Persistence.Entities;

namespace KF.RestApi.Common.Persistence.Repositories;

public interface IApiAuditRepository
{
    Task SaveAsync(ApiCallAudit audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiCallAudit>> GetRecentAsync(
        string apiName,
        int take,
        CancellationToken cancellationToken = default);
}
