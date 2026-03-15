using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KF.Web.Authorization.Core;

/// <summary>
/// Represents a secondary authorization check that can inspect the current HTTP context and user.
/// </summary>
public interface IContextAuthorizationCondition
{
    /// <summary>
    /// Evaluates whether the request should continue after the primary role rule succeeds.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="user">The authenticated user principal.</param>
    /// <param name="cancellationToken">A cancellation token that propagates request abort signals.</param>
    /// <returns><c>true</c> to allow the request; otherwise, <c>false</c>.</returns>
    ValueTask<bool> EvaluateAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);
}
