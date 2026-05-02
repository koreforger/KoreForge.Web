using Microsoft.AspNetCore.Http;

namespace KoreForge.Web.Authorization.Dynamic;

/// <summary>
/// Defines the dynamic authorization contract that determines whether a request may proceed.
/// </summary>
public interface IRequestPermissionEvaluator
{
    /// <summary>
    /// Evaluates the current request and returns whether it is authorized.
    /// </summary>
    /// <param name="httpContext">The active HTTP context.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns><c>true</c> if the request should continue; otherwise, <c>false</c>.</returns>
    Task<bool> IsAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
