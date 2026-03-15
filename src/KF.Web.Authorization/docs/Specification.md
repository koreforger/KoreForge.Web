# 1. Web-Authorization Specification

## 1.1 Goals

* Provide a **single, well-defined role semantics engine** with:

  * `AnyOf`, `AllOf`, `NotAnyOf`, `NotAllOf`.
* Support two **modes** of use:

  1. **Attribute mode** (“normal mode”): decorate controllers/actions.
  2. **Dynamic mode**: configure method permissions via an `IEnumerable<MethodPermissionRule>` (from DB, config, etc.).
* Both modes optionally support a **second-level check**:

  * A condition that inspects `HttpContext` and `ClaimsPrincipal` before allowing.
* Provide a small **sample ASP.NET Core API** that:

  * Generates JWTs for test users with different roles.
  * Has endpoints protected via both modes and conditions.
  * Lets you manually test the different semantics.

Assume:

* .NET 8+
* ASP.NET Core minimal hosting.
* JWT Bearer configured so that **roles** in the token become `ClaimTypes.Role` claims on `HttpContext.User`.

---

# 2. Core Concepts & Semantics

Let:

* `U` = set of roles for the current user.
* `R` = set of roles in a rule.

All checks are case-insensitive on role names.

**Role semantics:**

* `AnyOf`
  *Allow* if the user has **any** of the roles.
  `Allow = (U ∩ R ≠ ∅)`

* `AllOf`
  *Allow* if the user has **all** of the roles.
  `Allow = (R ⊆ U)`

* `NotAnyOf`
  *Allow* if the user has **none** of the roles.
  `Allow = (U ∩ R = ∅)`
  Typical use: “everyone except these roles”.

* `NotAllOf`
  *Allow* if the user does **not** have all of the roles.
  `Allow = NOT (R ⊆ U)`
  Typical use: “deny only this specific combination of roles (conflict of interest), allow all others”.

**Empty role set:**

* If a rule’s roles collection is empty: **allow** (design choice).

**Conditions (second level):**

* Optional, per rule.
* Signature:
  `ValueTask<bool> Condition(HttpContext context, ClaimsPrincipal user, CancellationToken token)`
* **Final result** for a rule:
  `Allow = RoleSemanticsSatisfied && (Condition == null || Condition(context, user, token))`

---

# 3. Project Layout

Example solution structure:

* `KhaosKode.Web.Authorization.Core`

  * `RoleRuleKind`
  * `IRoleAuthorizationService`, `RoleAuthorizationService`
  * `IContextAuthorizationCondition`
  * Attribute-based mode:

    * `RolesAuthorizeAttribute`
    * `RolesAuthorizationFilter`
  * DI extension: `AddRoleAuthorizationCore()`

* `KhaosKode.Web.Authorization.Dynamic`

  * Delegates & models:

    * `PermissionConditionDelegate`
    * `MethodPermissionRule`
    * `MethodKey`
  * Store:

    * `IMethodPermissionStore`
    * `InMemoryMethodPermissionStore`
  * Evaluator:

    * `IRequestPermissionEvaluator` (async)
    * `DefaultRequestPermissionEvaluator`
  * Middleware:

    * `PermissionsAuthorizationMiddleware`
  * DI extensions:

    * `AddDynamicMethodAuthorization(IEnumerable<MethodPermissionRule>)`
    * `UseDynamicMethodAuthorization()`

* `KhaosKode.Web.Authorization.SampleApi`

  * ASP.NET Core minimal API / MVC:

    * JWT generation endpoint
    * Sample protected endpoints for all modes/semantics
    * Uses both `KhaosKode.Web.Authorization.Core` and `KhaosKode.Web.Authorization.Dynamic`.

* Test projects (xUnit + FluentAssertions):

    * `KhaosKode.Web.Authorization.Core.Tests`
    * `KhaosKode.Web.Authorization.Dynamic.Tests`
    * `KhaosKode.Web.Authorization.SampleApi.Tests` (integration tests)

---

# 4. KhaosKode.Web.Authorization.Core Specification

## 4.1 RoleRuleKind

```csharp
namespace KhaosKode.Web.Authorization.Core;

public enum RoleRuleKind
{
    AnyOf,      // User must have >= 1 of R
    AllOf,      // User must have all of R
    NotAnyOf,   // User must have none of R
    NotAllOf    // User must NOT have all of R
}
```

## 4.2 IRoleAuthorizationService & RoleAuthorizationService

```csharp
using System.Security.Claims;

namespace KhaosKode.Web.Authorization.Core;

public interface IRoleAuthorizationService
{
    bool IsAuthorized(
        ClaimsPrincipal user,
        RoleRuleKind rule,
        IReadOnlyCollection<string> roles);
}
```

Implementation:

* Validates `user` and `roles` not null.
* Normalizes roles (trim, drop empty).
* If no roles after normalization → allow.
* Extracts user’s roles from `ClaimTypes.Role`.

```csharp
using System.Security.Claims;

namespace KhaosKode.Web.Authorization.Core;

public sealed class RoleAuthorizationService : IRoleAuthorizationService
{
    public bool IsAuthorized(
        ClaimsPrincipal user,
        RoleRuleKind rule,
        IReadOnlyCollection<string> roles)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (roles is null) throw new ArgumentNullException(nameof(roles));

        var requiredRoles = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToArray();

        if (requiredRoles.Length == 0)
        {
            return true;
        }

        var userRoles = new HashSet<string>(
            user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value),
            StringComparer.OrdinalIgnoreCase);

        return rule switch
        {
            RoleRuleKind.AnyOf    => requiredRoles.Any(userRoles.Contains),
            RoleRuleKind.AllOf    => requiredRoles.All(userRoles.Contains),
            RoleRuleKind.NotAnyOf => !requiredRoles.Any(userRoles.Contains),
            RoleRuleKind.NotAllOf => !requiredRoles.All(userRoles.Contains),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unsupported rule.")
        };
    }
}
```

    All framework and sample components use `ILogger<T>` for diagnostics. Custom middleware or conditions added later should follow the same pattern to ensure consistent logging.

## 4.3 IContextAuthorizationCondition

Used for “normal mode” (attribute-driven) to define extra predicate logic via DI.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KhaosKode.Web.Authorization.Core;

public interface IContextAuthorizationCondition
{
    ValueTask<bool> EvaluateAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);
}
```

Consumers implement this for their custom logic (business hours, headers, etc.).

## 4.4 Attribute Mode

### 4.4.1 RolesAuthorizeAttribute

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KhaosKode.Web.Authorization.Core.Mvc;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RolesAuthorizeAttribute : TypeFilterAttribute
{
    public RolesAuthorizeAttribute(
        RoleRuleKind rule,
        string[] roles,
        Type? conditionType = null)
        : base(typeof(RolesAuthorizationFilter))
    {
        Arguments = new object[]
        {
            rule,
            roles ?? Array.Empty<string>(),
            conditionType
        };
    }
}
```

* `rule`: `RoleRuleKind` semantics.
* `roles`: set of roles.
* `conditionType`: optional `Type` implementing `IContextAuthorizationCondition`.

### 4.4.2 RolesAuthorizationFilter

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KhaosKode.Web.Authorization.Core.Mvc;

public sealed class RolesAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly RoleRuleKind _rule;
    private readonly IReadOnlyCollection<string> _roles;
    private readonly Type? _conditionType;
    private readonly IRoleAuthorizationService _roleAuthorizationService;
    private readonly IServiceProvider _serviceProvider;

    public RolesAuthorizationFilter(
        RoleRuleKind rule,
        string[] roles,
        Type? conditionType,
        IRoleAuthorizationService roleAuthorizationService,
        IServiceProvider serviceProvider)
    {
        _rule = rule;
        _roles = roles ?? Array.Empty<string>();
        _conditionType = conditionType;
        _roleAuthorizationService = roleAuthorizationService;
        _serviceProvider = serviceProvider;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user?.Identity is not { IsAuthenticated: true })
        {
            context.Result = new ForbidResult();
            return;
        }

        // 1. Role-based check
        if (!_roleAuthorizationService.IsAuthorized(user, _rule, _roles))
        {
            context.Result = new ForbidResult();
            return;
        }

        // 2. Optional condition
        if (_conditionType is not null)
        {
            var condition = _serviceProvider.GetService(_conditionType) as IContextAuthorizationCondition;
            if (condition is null)
            {
                // Design choice: log and forbid if the condition cannot be resolved.
                context.Result = new ForbidResult();
                return;
            }

            var allowed = await condition.EvaluateAsync(httpContext, user, httpContext.RequestAborted);
            if (!allowed)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
```

### 4.4.3 DI Extension

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace KhaosKode.Web.Authorization.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRoleAuthorizationCore(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddScoped<IRoleAuthorizationService, RoleAuthorizationService>();
        return services;
    }
}
```

        ### 4.4.4 Registering Condition Types

        Every `conditionType` supplied to `RolesAuthorizeAttribute` must be registered with DI as a service implementing `IContextAuthorizationCondition`. Register each concrete condition as `Scoped` (safe access to request services) or `Transient` if it is stateless. Example:

        ```csharp
        services.AddScoped<BusinessHoursCondition>();
        services.AddTransient<GeofencingCondition>();
        ```

        Failing to register a condition type causes the filter to return `ForbidResult`, so the spec explicitly requires developers to add each condition to `IServiceCollection` during application startup.

---

# 5. KhaosKode.Web.Authorization.Dynamic Specification

## 5.1 Delegates & Models

### 5.1.1 PermissionConditionDelegate

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KhaosKode.Web.Authorization.Dynamic;

public delegate ValueTask<bool> PermissionConditionDelegate(
    HttpContext httpContext,
    ClaimsPrincipal user,
    CancellationToken cancellationToken);
```

### 5.1.2 MethodPermissionRule

```csharp
using KhaosKode.Web.Authorization.Core;

namespace KhaosKode.Web.Authorization.Dynamic;

public sealed class MethodPermissionRule
{
    public string TypeFullName { get; }
    public string MethodName { get; }

    public RoleRuleKind RuleKind { get; }
    public IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Optional extra condition evaluated after roles succeed.
    /// </summary>
    public PermissionConditionDelegate? Condition { get; }

    public MethodPermissionRule(
        string typeFullName,
        string methodName,
        RoleRuleKind ruleKind,
        IEnumerable<string> roles,
        PermissionConditionDelegate? condition = null)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
            throw new ArgumentException("Type full name is required.", nameof(typeFullName));
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name is required.", nameof(methodName));

        TypeFullName = typeFullName.Trim();
        MethodName = methodName.Trim();
        RuleKind = ruleKind;

        Roles = roles?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToArray()
            ?? Array.Empty<string>();

        Condition = condition;
    }
}
```

### 5.1.3 MethodKey

```csharp
namespace KhaosKode.Web.Authorization.Dynamic;

public readonly struct MethodKey : IEquatable<MethodKey>
{
    public string TypeFullName { get; }
    public string MethodName { get; }

    public MethodKey(string typeFullName, string methodName)
    {
        TypeFullName = typeFullName ?? throw new ArgumentNullException(nameof(typeFullName));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    public bool Equals(MethodKey other) =>
        string.Equals(TypeFullName, other.TypeFullName, StringComparison.Ordinal) &&
        string.Equals(MethodName, other.MethodName, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is MethodKey other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(TypeFullName),
            StringComparer.Ordinal.GetHashCode(MethodName));

    public override string ToString() => $"{TypeFullName}::{MethodName}";
}
```

## 5.2 Method Permission Store

### 5.2.1 IMethodPermissionStore

```csharp
namespace KhaosKode.Web.Authorization.Dynamic;

public interface IMethodPermissionStore
{
    IReadOnlyCollection<MethodPermissionRule> GetRules(MethodKey methodKey);
}
```

### 5.2.2 InMemoryMethodPermissionStore

```csharp
namespace KhaosKode.Web.Authorization.Dynamic;

public sealed class InMemoryMethodPermissionStore : IMethodPermissionStore
{
    private readonly IReadOnlyDictionary<MethodKey, IReadOnlyCollection<MethodPermissionRule>> _rulesByKey;

    public InMemoryMethodPermissionStore(IEnumerable<MethodPermissionRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        _rulesByKey = rules
            .GroupBy(r => new MethodKey(r.TypeFullName, r.MethodName))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<MethodPermissionRule>)g.ToArray());
    }

    public IReadOnlyCollection<MethodPermissionRule> GetRules(MethodKey methodKey)
    {
        if (_rulesByKey.TryGetValue(methodKey, out var rules))
        {
            return rules;
        }

        return Array.Empty<MethodPermissionRule>();
    }
}
```

* Multiple rules per method are allowed.
* Evaluator will require **all** rules to pass (AND semantics).

## 5.3 Request Permission Evaluator

### 5.3.1 IRequestPermissionEvaluator

```csharp
using Microsoft.AspNetCore.Http;

namespace KhaosKode.Web.Authorization.Dynamic;

public interface IRequestPermissionEvaluator
{
    Task<bool> IsAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
```

### 5.3.2 DefaultRequestPermissionEvaluator

Uses `ControllerActionDescriptor` metadata to determine controller type and method name.

```csharp
using System.Reflection;
using System.Security.Claims;
using KhaosKode.Web.Authorization.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace KhaosKode.Web.Authorization.Dynamic;

public sealed class DefaultRequestPermissionEvaluator : IRequestPermissionEvaluator
{
    private readonly IMethodPermissionStore _permissionStore;
    private readonly IRoleAuthorizationService _roleAuthorizationService;

    public DefaultRequestPermissionEvaluator(
        IMethodPermissionStore permissionStore,
        IRoleAuthorizationService roleAuthorizationService)
    {
        _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        _roleAuthorizationService = roleAuthorizationService ?? throw new ArgumentNullException(nameof(roleAuthorizationService));
    }

    public async Task<bool> IsAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

        var user = httpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            // Design: if rules exist for this method, unauthenticated is not allowed.
            // If no rules, method is open; but that case is handled later.
        }

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            // No endpoint: allow (static files, etc.)
            return true;
        }

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor is null)
        {
            // Not an MVC controller action: allow by default.
            return true;
        }

        var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
        var methodInfo = actionDescriptor.MethodInfo;

        var key = new MethodKey(controllerType.FullName!, methodInfo.Name);

        var rules = _permissionStore.GetRules(key);
        if (rules.Count == 0)
        {
            // No rules: allow.
            return true;
        }

        if (user?.Identity is not { IsAuthenticated: true })
        {
            // There are rules and user is not authenticated -> deny.
            return false;
        }

        // Require all rules to pass.
        foreach (var rule in rules)
        {
            // 1. Role check
            var rolesOk = _roleAuthorizationService.IsAuthorized(user, rule.RuleKind, rule.Roles);
            if (!rolesOk)
            {
                return false;
            }

            // 2. Optional condition
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
```

## 5.4 Middleware

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KhaosKode.Web.Authorization.Dynamic;

public sealed class PermissionsAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionsAuthorizationMiddleware> _logger;
    private readonly IRequestPermissionEvaluator _permissionEvaluator;

    public PermissionsAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<PermissionsAuthorizationMiddleware> logger,
        IRequestPermissionEvaluator permissionEvaluator)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var authorized = await _permissionEvaluator.IsAuthorizedAsync(context, context.RequestAborted);
        if (!authorized)
        {
            _logger.LogWarning("Request to {Path} forbidden by dynamic authorization.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
```

## 5.5 DI Extensions

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KhaosKode.Web.Authorization.Dynamic;

public static class DynamicAuthorizationExtensions
{
    public static IServiceCollection AddDynamicMethodAuthorization(
        this IServiceCollection services,
        IEnumerable<MethodPermissionRule> rules)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        services.AddSingleton<IMethodPermissionStore>(_ => new InMemoryMethodPermissionStore(rules));
        services.AddScoped<IRequestPermissionEvaluator, DefaultRequestPermissionEvaluator>();
        return services;
    }

    public static IApplicationBuilder UseDynamicMethodAuthorization(this IApplicationBuilder app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        return app.UseMiddleware<PermissionsAuthorizationMiddleware>();
    }
}
```

---

# 6. Sample API Specification (KhaosKode.Web.Authorization.SampleApi)

## 6.1 Purpose

* Provide a small ASP.NET Core Web API that:

  * Issues JWTs with different roles.
  * Has endpoints protected via:

    * Attribute mode (RolesAuthorizeAttribute + `IContextAuthorizationCondition`).
    * Dynamic mode (MethodPermissionRule + PermissionConditionDelegate).
  * Demonstrates `AnyOf`, `AllOf`, `NotAnyOf`, `NotAllOf` with and without conditions.

## 6.2 JWT Generation

### 6.2.1 Symmetric key

* Use a static symmetric key (e.g. config `Jwt:Key`).
* Issuer/audience fixed (e.g. `SampleAuth`).

### 6.2.2 /auth/token Endpoint

* Input DTO:

```csharp
public sealed class TokenRequest
{
    public string UserName { get; set; } = default!;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
```

* Implementation:

  * Validate input.
  * Issue JWT with:

    * `sub` = username
    * `role` claims = roles supplied
  * 1-hour expiry.

* Endpoint: `POST /auth/token`

### 6.2.3 JWT auth configuration

* Configure JWT Bearer:

  * `TokenValidationParameters.RoleClaimType` set to `"role"` or `"roles"`.
  * That resolves to `ClaimTypes.Role` for our core library.

Configuration values such as symmetric keys, issuer, and audience live in `appsettings.json` / `appsettings.Development.json` as plain strings for now. Future hardening (Key Vault, user secrets, etc.) can be layered later without changing the API surface.

## 6.3 Sample Controllers / Endpoints

### 6.3.1 Attribute mode demo controller

`AttributeDemoController` with endpoints:

1. `GET /api/attr/admin-or-support`

   * `[RolesAuthorize(RoleRuleKind.AnyOf, new[] { "Admin", "Support" })]`
   * Returns 200 if user has Admin or Support.

2. `GET /api/attr/admin-and-supervisor`

   * `[RolesAuthorize(RoleRuleKind.AllOf, new[] { "Admin", "Supervisor" })]`

3. `GET /api/attr/everyone-except-suspended`

   * `[RolesAuthorize(RoleRuleKind.NotAnyOf, new[] { "Suspended" })]`

4. `GET /api/attr/not-trader-and-auditor`

   * `[RolesAuthorize(RoleRuleKind.NotAllOf, new[] { "Trader", "Auditor" })]`
   * Blocks only if user has both roles.

5. `GET /api/attr/business-hours-only`

   * `[RolesAuthorize(RoleRuleKind.AnyOf, new[] { "User", "Admin" }, typeof(BusinessHoursCondition))]`

### 6.3.2 BusinessHoursCondition

Implements `IContextAuthorizationCondition`:

* Allowed hours: 08:00–17:00 local (or UTC for simplicity).
* If outside that, return false.

### 6.3.3 Dynamic mode demo controller

`DynamicDemoController` with endpoints:

1. `GET /api/dyn/orders/view`

   * Require `AnyOf` { "Admin", "Sales" }.
   * No extra condition.

2. `POST /api/dyn/orders/create`

   * Require `AllOf` { "Admin", "Sales" }.
   * Condition: header `X-Request-Source` must be `"Internal"`.

3. `DELETE /api/dyn/orders/{id}`

   * Require `AnyOf` { "Admin" }.
   * Condition: only allowed during business hours.

4. `GET /api/dyn/reports/sensitive`

   * `NotAnyOf` { "Suspended", "Blacklisted" }.
   * Condition: query parameter `?tenantId` must match a claim `tenant_id`.

## 6.4 Dynamic Rules Configuration in Program.cs

Example:

```csharp
using KhaosKode.Web.Authorization.Core;
using KhaosKode.Web.Authorization.Dynamic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// JWT auth configured...

builder.Services.AddRoleAuthorizationCore();

// dynamic rules
var rules = new List<MethodPermissionRule>
{
    new(
        typeFullName: "KhaosKode.Web.Authorization.SampleApi.Controllers.DynamicDemoController",
        methodName: "ViewOrders",
        ruleKind: RoleRuleKind.AnyOf,
        roles: new[] { "Admin", "Sales" }),

    new(
        typeFullName: "KhaosKode.Web.Authorization.SampleApi.Controllers.DynamicDemoController",
        methodName: "CreateOrder",
        ruleKind: RoleRuleKind.AllOf,
        roles: new[] { "Admin", "Sales" },
        condition: async (ctx, user, ct) =>
        {
            var headerValue = ctx.Request.Headers["X-Request-Source"].ToString();
            return await ValueTask.FromResult(string.Equals(headerValue, "Internal", StringComparison.OrdinalIgnoreCase));
        }),

    new(
        typeFullName: "KhaosKode.Web.Authorization.SampleApi.Controllers.DynamicDemoController",
        methodName: "DeleteOrder",
        ruleKind: RoleRuleKind.AnyOf,
        roles: new[] { "Admin" },
        condition: async (ctx, user, ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            var hour = now.Hour;
            var allowed = hour >= 8 && hour <= 17;
            return await ValueTask.FromResult(allowed);
        }),

    new(
        typeFullName: "KhaosKode.Web.Authorization.SampleApi.Controllers.DynamicDemoController",
        methodName: "GetSensitiveReport",
        ruleKind: RoleRuleKind.NotAnyOf,
        roles: new[] { "Suspended", "Blacklisted" },
        condition: async (ctx, user, ct) =>
        {
            var tenantFromQuery = ctx.Request.Query["tenantId"].ToString();
            var tenantFromClaim = user.FindFirst("tenant_id")?.Value;
            var ok = !string.IsNullOrEmpty(tenantFromQuery)
                     && string.Equals(tenantFromQuery, tenantFromClaim, StringComparison.Ordinal);
            return await ValueTask.FromResult(ok);
        })
};

builder.Services.AddDynamicMethodAuthorization(rules);

var app = builder.Build();

app.UseAuthentication();
app.UseRouting();

app.UseDynamicMethodAuthorization();

app.UseAuthorization();
app.MapControllers();

app.Run();
```

*Controller scope:* `DefaultRequestPermissionEvaluator` only needs to resolve permissions for MVC controller actions via `ControllerActionDescriptor`. Minimal APIs, gRPC endpoints, and other middleware-driven pipelines fall through this component and are treated as allowed.

*Allow-by-default rule:* As reiterated above, if no explicit rule exists for a controller action, the request is allowed even for anonymous users. Only endpoints with at least one rule require authentication.

---

# 7. Unit Test Specification

Use xUnit + FluentAssertions.

## 7.1 KhaosKode.Web.Authorization.Core.Tests

### 7.1.1 RoleAuthorizationServiceTests

**Test cases:**

1. `AnyOf_UserHasOneRequiredRole_ReturnsTrue`
2. `AnyOf_UserHasNoneOfRequiredRoles_ReturnsFalse`
3. `AllOf_UserHasAllRoles_ReturnsTrue`
4. `AllOf_UserMissingAtLeastOneRole_ReturnsFalse`
5. `NotAnyOf_UserHasNoneRoles_ReturnsTrue`
6. `NotAnyOf_UserHasForbiddenRole_ReturnsFalse`
7. `NotAllOf_UserHasAllRoles_ReturnsFalse`
8. `NotAllOf_UserMissingOneRole_ReturnsTrue`
9. `EmptyRoles_ReturnsTrueRegardlessOfUserRoles`
10. `RoleComparison_IsCaseInsensitive`
11. `NullUser_ThrowsArgumentNullException`
12. `NullRoles_ThrowsArgumentNullException`

Each creates a `ClaimsPrincipal` with specific roles and asserts expected bool.

### 7.1.2 RolesAuthorizationFilterTests

Focus on combination of:

* Role semantics.
* Optional condition.
* Missing condition resolution.

**Test cases:**

1. `UnauthenticatedUser_Forbids`
2. `RolesFail_Forbids_ConditionNotEvaluated`
3. `RolesPass_NoCondition_Allows`
4. `RolesPass_ConditionReturnsTrue_Allows`
5. `RolesPass_ConditionReturnsFalse_Forbids`
6. `ConditionTypeNotRegistered_Forbids`

Use a fake `IContextAuthorizationCondition` which tracks calls and returns a configurable result.

## 7.2 KhaosKode.Web.Authorization.Dynamic.Tests

### 7.2.1 InMemoryMethodPermissionStoreTests

1. `GetRules_KeyExists_ReturnsAllRulesForKey`
2. `GetRules_KeyMissing_ReturnsEmptyCollection`
3. `MultipleRulesForSameMethod_AreGroupedTogether`

### 7.2.2 DefaultRequestPermissionEvaluatorTests

You’ll create a helper:

```csharp
HttpContext CreateHttpContext(
    string controllerTypeFullName,
    string methodName,
    string[] roles,
    bool authenticated = true);
```

That:

* Creates `DefaultHttpContext`.
* Sets `User` with specified roles.
* Constructs an `Endpoint` with `ControllerActionDescriptor` metadata.

**Test cases:**

1. `NoEndpoint_ReturnsTrue`
2. `NonControllerEndpoint_ReturnsTrue`
3. `NoRulesForMethod_ReturnsTrue`
4. `SingleRule_RolesPass_NoCondition_ReturnsTrue`
5. `SingleRule_RolesFail_ReturnsFalse`
6. `SingleRule_RolesPass_ConditionTrue_ReturnsTrue`
7. `SingleRule_RolesPass_ConditionFalse_ReturnsFalse`
8. `MultipleRules_AllPass_ReturnsTrue`
9. `MultipleRules_OneRoleFails_ReturnsFalse`
10. `MultipleRules_OneConditionFails_ReturnsFalse`
11. `UnauthenticatedUser_WithRules_ReturnsFalse`

### 7.2.3 PermissionsAuthorizationMiddlewareTests

Using a mock `IRequestPermissionEvaluator`:

1. `AuthorizedRequest_CallsNext`
2. `UnauthorizedRequest_DoesNotCallNext_Sets403`

---

## 7.3 KhaosKode.Web.Authorization.SampleApi.Tests (Integration)

Use `WebApplicationFactory<Program>` to spin up the sample API.

### 7.3.1 TokenHelper

* Helper to call `/auth/token` and get JWT for given roles.

### 7.3.2 Scenarios

1. **Attribute: AdminOrSupport**

   * Token with `["Admin"]` → GET `/api/attr/admin-or-support` → 200.
   * Token with `["Support"]` → 200.
   * Token with `["User"]` → 403.

2. **Attribute: NotAnyOf Suspended**

   * Token with `["User"]` → 200.
   * Token with `["Suspended"]` → 403.

3. **Attribute: NotAllOf Trader+Auditor**

   * Token with `["Trader"]` → 200.
   * Token with `["Auditor"]` → 200.
   * Token with `["Trader", "Auditor"]` → 403.

4. **Attribute: BusinessHoursCondition**

   * If you stub clock or temporarily adjust condition:

     * When condition returns true → 200.
     * When condition returns false → 403.

5. **Dynamic: ViewOrders (AnyOf Admin/Sales)**

   * Token with `["Admin"]` or `["Sales"]` → 200.
   * Token with `["User"]` → 403.

6. **Dynamic: CreateOrder With Header Condition**

   * Token: `["Admin", "Sales"]`, header `X-Request-Source: Internal` → 200.
   * Same roles, missing header → 403.
   * Header present, roles don’t satisfy `AllOf` → 403.

7. **Dynamic: SensitiveReport With NotAnyOf + Tenant Condition**

   * Token: `["User"]`, claim `tenant_id=123`, query `?tenantId=123` → 200.
   * Same token, query `?tenantId=999` → 403.
   * Token with `["Suspended"]` → 403 regardless of tenant.

8. **NoToken_WhenRulesExist_Returns403**

   * Call dynamic endpoint with rules but no token → 403.

These integration tests validate the whole path: JWT generation → authentication → attribute/dynamic enforcement → response.


