using System.Security.Claims;
using FluentAssertions;
using KF.Web.Authorization.Core;
using KF.Web.Authorization.Core.Mvc;
using KF.Web.Authorization.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KF.Web.Authorization.Tests.Core;

public class RolesAuthorizationFilterTests
{
    [Fact]
    public async Task UnauthenticatedUser_Forbids()
    {
        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(null),
            roleAuthorizationService: new StubRoleAuthorizationService((_, _, _) => true),
            serviceProvider: new ServiceCollection().BuildServiceProvider());

        var context = CreateContext(authenticated: false);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RolesFail_Forbids_ConditionNotEvaluated()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TrackingCondition>();
        var provider = services.BuildServiceProvider();

        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(typeof(TrackingCondition)),
            new StubRoleAuthorizationService((_, _, _) => false),
            provider);

        var context = CreateContext();
        var condition = provider.GetRequiredService<TrackingCondition>();

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ForbidResult>();
        condition.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task RolesPass_NoCondition_Allows()
    {
        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(null),
            new StubRoleAuthorizationService((_, _, _) => true),
            new ServiceCollection().BuildServiceProvider());

        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task RolesPass_ConditionReturnsTrue_Allows()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TrackingCondition>(_ => new TrackingCondition(true));
        var provider = services.BuildServiceProvider();

        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(typeof(TrackingCondition)),
            new StubRoleAuthorizationService((_, _, _) => true),
            provider);

        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
        provider.GetRequiredService<TrackingCondition>().InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task RolesPass_ConditionReturnsFalse_Forbids()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TrackingCondition>(_ => new TrackingCondition(false));
        var provider = services.BuildServiceProvider();

        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(typeof(TrackingCondition)),
            new StubRoleAuthorizationService((_, _, _) => true),
            provider);

        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ConditionTypeNotRegistered_Forbids()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var filter = new RolesAuthorizationFilter(
            RoleRuleKind.AnyOf,
            new[] { "Admin" },
            new RolesAuthorizeAttribute.ConditionTypeHolder(typeof(TrackingCondition)),
            new StubRoleAuthorizationService((_, _, _) => true),
            provider);

        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    private static AuthorizationFilterContext CreateContext(bool authenticated = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = authenticated
            ? ClaimsPrincipalFactory.Create("Admin")
            : new ClaimsPrincipal(new ClaimsIdentity());

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private sealed class StubRoleAuthorizationService : IRoleAuthorizationService
    {
        private readonly Func<ClaimsPrincipal, RoleRuleKind, IReadOnlyCollection<string>, bool> _impl;

        public StubRoleAuthorizationService(Func<ClaimsPrincipal, RoleRuleKind, IReadOnlyCollection<string>, bool> impl)
        {
            _impl = impl;
        }

        public bool IsAuthorized(ClaimsPrincipal user, RoleRuleKind rule, IReadOnlyCollection<string> roles) => _impl(user, rule, roles);
    }

    private sealed class TrackingCondition : KF.Web.Authorization.Core.IContextAuthorizationCondition
    {
        private readonly bool _result;

        public TrackingCondition() : this(true)
        {
        }

        public TrackingCondition(bool result)
        {
            _result = result;
        }

        public int InvocationCount { get; private set; }

        public ValueTask<bool> EvaluateAsync(HttpContext httpContext, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return ValueTask.FromResult(_result);
        }
    }
}
