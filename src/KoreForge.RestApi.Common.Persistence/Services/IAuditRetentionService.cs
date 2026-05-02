using System.Threading;
using System.Threading.Tasks;

namespace KoreForge.RestApi.Common.Persistence.Services;

public interface IAuditRetentionService
{
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}