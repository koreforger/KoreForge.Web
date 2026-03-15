using System.Linq;
using KF.RestApi.Common.Persistence.Entities;
using KF.RestApi.Common.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KF.RestApi.Common.Persistence.Repositories;

internal sealed class ApiAuditRepository : IApiAuditRepository
{
    private readonly ApiGatewayDbContext _context;
    private readonly AuditStoreOptions _options;

    public ApiAuditRepository(ApiGatewayDbContext context, IOptions<AuditStoreOptions> auditOptions)
    {
        _context = context;
        _options = auditOptions.Value;
    }

    public async Task SaveAsync(ApiCallAudit audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audit);

        if (string.Equals(_options.TableMode, "Split", StringComparison.OrdinalIgnoreCase))
        {
            await SaveSplitAsync(audit, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _context.ApiCallAudits.Add(audit);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ApiCallAudit>> GetRecentAsync(
        string apiName,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        return await _context.ApiCallAudits
            .AsNoTracking()
            .Where(x => x.ApiName == apiName)
            .OrderByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SaveSplitAsync(ApiCallAudit audit, CancellationToken cancellationToken)
    {
        var request = new ApiCallRequestAudit
        {
            ApiName = audit.ApiName,
            Operation = audit.Operation,
            Direction = "Request",
            CallerSystem = audit.CallerSystem,
            StatusCode = audit.StatusCode,
            ErrorCode = audit.ErrorCode,
            ErrorMessage = audit.ErrorMessage,
            HttpMethod = audit.HttpMethod,
            RequestPath = audit.RequestPath,
            RequestPayload = audit.RequestPayload,
            RequestTimestampUtc = audit.RequestTimestampUtc,
            ResponseTimestampUtc = audit.ResponseTimestampUtc,
            DurationMs = audit.DurationMs,
            CorrelationId = audit.CorrelationId
        };

        var response = new ApiCallResponseAudit
        {
            ApiName = audit.ApiName,
            Operation = audit.Operation,
            Direction = "Response",
            CallerSystem = audit.CallerSystem,
            StatusCode = audit.StatusCode,
            ErrorCode = audit.ErrorCode,
            ErrorMessage = audit.ErrorMessage,
            HttpMethod = audit.HttpMethod,
            RequestPath = audit.RequestPath,
            ResponsePayload = audit.ResponsePayload,
            RequestTimestampUtc = audit.RequestTimestampUtc,
            ResponseTimestampUtc = audit.ResponseTimestampUtc,
            DurationMs = audit.DurationMs,
            CorrelationId = audit.CorrelationId
        };

        _context.ApiCallRequestAudits.Add(request);
        _context.ApiCallResponseAudits.Add(response);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
