using KF.Web.Authorization.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace KF.Web.Authorization.Dynamic;

/// <summary>
/// Resolves controller action metadata and enforces configured <see cref="MethodPermissionRule"/> entries.
/// </summary>
public sealed class DefaultRequestPermissionEvaluator : IRequestPermissionEvaluator
{
    private readonly IMethodPermissionStore _permissionStore;
    private readonly IRoleAuthorizationService _roleAuthorizationService;

    /// <summary>
    /// Creates a new evaluator instance.
    /// </summary>
    /// <param name="permissionStore">Rule store used to lookup method permissions.</param>
    /// <param name="roleAuthorizationService">Role evaluator shared with attribute mode.</param>
    public DefaultRequestPermissionEvaluator(
        IMethodPermissionStore permissionStore,
        IRoleAuthorizationService roleAuthorizationService)
    {
        _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        _roleAuthorizationService = roleAuthorizationService ?? throw new ArgumentNullException(nameof(roleAuthorizationService));
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (httpContext is null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return true;
        }

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor is null)
        {
            return true;
        }

        var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
        var methodInfo = actionDescriptor.MethodInfo;

        var key = new MethodKey(controllerType.FullName!, methodInfo.Name);
        var rules = _permissionStore.GetRules(key);

        if (rules.Count == 0)
        {
            return true;
        }

        var user = httpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            return false;
        }

        foreach (var rule in rules)
        {
            var rolesOk = _roleAuthorizationService.IsAuthorized(user, rule.RuleKind, rule.Roles);
            if (!rolesOk)
            {
                return false;
            }

            if (rule.Condition is not null)
            {
                var allowed = await rule.Condition(httpContext, user, cancellationToken);
                if (!allowed)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
