using System.Security.Claims;
using KF.Web.Authorization.Core;
using Microsoft.AspNetCore.Http;

namespace KF.Web.Authorization.Sample.Conditions;

public sealed class BusinessHoursCondition : IContextAuthorizationCondition
{
    private readonly TimeSpan _start = new(8, 0, 0);
    private readonly TimeSpan _end = new(17, 0, 0);
    private readonly TimeProvider _timeProvider;

    public BusinessHoursCondition(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public ValueTask<bool> EvaluateAsync(HttpContext httpContext, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().TimeOfDay;
        var allowed = now >= _start && now <= _end;
        return ValueTask.FromResult(allowed);
    }
}
