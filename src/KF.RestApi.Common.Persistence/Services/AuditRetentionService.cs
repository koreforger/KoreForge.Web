using KF.RestApi.Common.Persistence.Entities;
using KF.RestApi.Common.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KF.RestApi.Common.Persistence.Services;

internal sealed class AuditRetentionService : IAuditRetentionService
{
    private readonly ApiGatewayDbContext _context;
    private readonly AuditStoreOptions _options;

    public AuditRetentionService(ApiGatewayDbContext context, IOptions<AuditStoreOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.RetentionDays.HasValue)
        {
            return 0;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays.Value);
        var total = 0;

        var tableMode = _options.TableMode ?? "Single";
        if (string.Equals(tableMode, "Split", StringComparison.OrdinalIgnoreCase))
        {
            total += await PurgeAsync<ApiCallRequestAudit>(_context.ApiCallRequestAudits, cutoff, cancellationToken).ConfigureAwait(false);
            total += await PurgeAsync<ApiCallResponseAudit>(_context.ApiCallResponseAudits, cutoff, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            total += await PurgeAsync<ApiCallAudit>(_context.ApiCallAudits, cutoff, cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    private static async Task<int> PurgeAsync<TEntity>(DbSet<TEntity> set, DateTimeOffset cutoff, CancellationToken cancellationToken)
        where TEntity : class, IResponseTimestamp
    {
        return await set.Where(x => x.ResponseTimestampUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
