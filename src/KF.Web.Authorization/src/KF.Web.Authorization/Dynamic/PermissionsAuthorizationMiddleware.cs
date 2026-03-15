using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KF.Web.Authorization.Dynamic;

/// <summary>
/// Middleware that blocks requests failing dynamic authorization rules.
/// </summary>
public sealed class PermissionsAuthorizationMiddleware : IMiddleware
{
    private readonly ILogger<PermissionsAuthorizationMiddleware> _logger;
    private readonly IRequestPermissionEvaluator _permissionEvaluator;

    /// <summary>
    /// Creates a middleware instance.
    /// </summary>
    /// <param name="logger">Logger used for denial diagnostics.</param>
    /// <param name="permissionEvaluator">Evaluator that enforces <see cref="MethodPermissionRule"/> entries.</param>
    public PermissionsAuthorizationMiddleware(
        ILogger<PermissionsAuthorizationMiddleware> logger,
        IRequestPermissionEvaluator permissionEvaluator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (next is null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        var authorized = await _permissionEvaluator.IsAuthorizedAsync(context, context.RequestAborted);
        if (!authorized)
        {
            _logger.LogWarning("Request to {Path} forbidden by dynamic authorization.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
