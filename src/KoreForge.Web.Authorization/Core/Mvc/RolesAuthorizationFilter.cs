using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KoreForge.Web.Authorization.Core.Mvc;

/// <summary>
/// MVC authorization filter that enforces <see cref="RoleRuleKind"/> semantics and optional conditions.
/// </summary>
public sealed class RolesAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly RoleRuleKind _rule;
    private readonly IReadOnlyCollection<string> _roles;
    private readonly Type? _conditionType;
    private readonly IRoleAuthorizationService _roleAuthorizationService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="RolesAuthorizationFilter"/> instance.
    /// </summary>
    /// <param name="rule">Role semantics to enforce.</param>
    /// <param name="roles">Roles supplied via the attribute.</param>
    /// <param name="conditionTypeHolder">Optional condition type configured on the attribute.</param>
    /// <param name="roleAuthorizationService">Role evaluation service.</param>
    /// <param name="serviceProvider">The request service provider.</param>
    public RolesAuthorizationFilter(
        RoleRuleKind rule,
        string[] roles,
        RolesAuthorizeAttribute.ConditionTypeHolder conditionTypeHolder,
        IRoleAuthorizationService roleAuthorizationService,
        IServiceProvider serviceProvider)
    {
        _rule = rule;
        _roles = roles ?? Array.Empty<string>();
        _conditionType = conditionTypeHolder?.Value;
        _roleAuthorizationService = roleAuthorizationService ?? throw new ArgumentNullException(nameof(roleAuthorizationService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Executes the role + condition checks for the current request.
    /// </summary>
    /// <param name="context">The MVC authorization filter context.</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user?.Identity is not { IsAuthenticated: true })
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!_roleAuthorizationService.IsAuthorized(user, _rule, _roles))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (_conditionType is null)
        {
            return;
        }

        var condition = _serviceProvider.GetService(_conditionType) as IContextAuthorizationCondition;
        if (condition is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        var allowed = await condition.EvaluateAsync(httpContext, user, httpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}
