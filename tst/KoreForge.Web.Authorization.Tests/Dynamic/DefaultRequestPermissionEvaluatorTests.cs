using FluentAssertions;
using KoreForge.Web.Authorization.Core;
using KoreForge.Web.Authorization.Dynamic;
using KoreForge.Web.Authorization.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;

namespace KoreForge.Web.Authorization.Tests.Dynamic;

public class DefaultRequestPermissionEvaluatorTests
{
    private static readonly Type ControllerType = typeof(TestController);

    [Fact]
    public async Task NoEndpoint_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(Array.Empty<MethodPermissionRule>());
        var context = new DefaultHttpContext();

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task NonControllerEndpoint_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(Array.Empty<MethodPermissionRule>());
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "test"));

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task NoRulesForMethod_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(Array.Empty<MethodPermissionRule>());
        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.OpenEndpoint), new[] { "User" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SingleRule_RolesPass_NoCondition_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(nameof(TestController.ProtectedEndpoint), RoleRuleKind.AnyOf, new[] { "Admin" })
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.ProtectedEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SingleRule_RolesFail_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(nameof(TestController.ProtectedEndpoint), RoleRuleKind.AnyOf, new[] { "Admin" })
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.ProtectedEndpoint), new[] { "User" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SingleRule_RolesPass_ConditionTrue_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(
                nameof(TestController.ProtectedEndpoint),
                RoleRuleKind.AnyOf,
                new[] { "Admin" },
                condition: (_, _, _) => ValueTask.FromResult(true))
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.ProtectedEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SingleRule_RolesPass_ConditionFalse_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(
                nameof(TestController.ProtectedEndpoint),
                RoleRuleKind.AnyOf,
                new[] { "Admin" },
                condition: (_, _, _) => ValueTask.FromResult(false))
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.ProtectedEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleRules_AllPass_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(nameof(TestController.MultiRuleEndpoint), RoleRuleKind.AnyOf, new[] { "Admin" }),
            CreateRule(nameof(TestController.MultiRuleEndpoint), RoleRuleKind.NotAnyOf, new[] { "Suspended" })
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.MultiRuleEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleRules_OneRoleFails_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(nameof(TestController.MultiRuleEndpoint), RoleRuleKind.AllOf, new[] { "Admin", "Sales" }),
            CreateRule(nameof(TestController.MultiRuleEndpoint), RoleRuleKind.NotAnyOf, new[] { "Suspended" })
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.MultiRuleEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleRules_OneConditionFails_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(
                nameof(TestController.MultiRuleEndpoint),
                RoleRuleKind.AnyOf,
                new[] { "Admin" },
                condition: (_, _, _) => ValueTask.FromResult(true)),
            CreateRule(
                nameof(TestController.MultiRuleEndpoint),
                RoleRuleKind.AnyOf,
                new[] { "Admin" },
                condition: (_, _, _) => ValueTask.FromResult(false))
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.MultiRuleEndpoint), new[] { "Admin" });

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnauthenticatedUser_WithRules_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(new[]
        {
            CreateRule(nameof(TestController.ProtectedEndpoint), RoleRuleKind.AnyOf, new[] { "Admin" })
        });

        var context = ControllerEndpointFactory.CreateContext(ControllerType, nameof(TestController.ProtectedEndpoint), Array.Empty<string>(), authenticated: false);

        var result = await evaluator.IsAuthorizedAsync(context, CancellationToken.None);

        result.Should().BeFalse();
    }

    private static DefaultRequestPermissionEvaluator CreateEvaluator(IEnumerable<MethodPermissionRule> rules)
    {
        var store = new InMemoryMethodPermissionStore(rules);
        return new DefaultRequestPermissionEvaluator(store, new RoleAuthorizationService());
    }

    private static MethodPermissionRule CreateRule(
        string methodName,
        RoleRuleKind kind,
        IEnumerable<string> roles,
        PermissionConditionDelegate? condition = null) =>
        new(ControllerType.FullName!, methodName, kind, roles, condition);

    private sealed class TestController
    {
        public void OpenEndpoint()
        {
        }

        public void ProtectedEndpoint()
        {
        }

        public void MultiRuleEndpoint()
        {
        }
    }
}
