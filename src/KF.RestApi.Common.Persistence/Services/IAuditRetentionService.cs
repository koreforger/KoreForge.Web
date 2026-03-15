using System.Threading;
using System.Threading.Tasks;

namespace KF.RestApi.Common.Persistence.Services;

public interface IAuditRetentionService
{
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}