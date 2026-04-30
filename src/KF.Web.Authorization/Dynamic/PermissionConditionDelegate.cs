using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KF.Web.Authorization.Dynamic;

/// <summary>
/// Represents an asynchronous predicate evaluated after a rule's role check succeeds.
/// </summary>
/// <param name="httpContext">The active HTTP context.</param>
/// <param name="user">The authenticated user.</param>
/// <param name="cancellationToken">Token that signals request abortion.</param>
/// <returns><c>true</c> to authorize the request; otherwise, <c>false</c>.</returns>
public delegate ValueTask<bool> PermissionConditionDelegate(
    HttpContext httpContext,
    ClaimsPrincipal user,
    CancellationToken cancellationToken);
