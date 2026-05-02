using FluentAssertions;
using KoreForge.Web.Authorization.Core;
using KoreForge.Web.Authorization.Dynamic;

namespace KoreForge.Web.Authorization.Tests.Dynamic;

public class InMemoryMethodPermissionStoreTests
{
    [Fact]
    public void GetRules_KeyExists_ReturnsAllRulesForKey()
    {
        var rule = CreateRule("TypeA", "Method", RoleRuleKind.AnyOf, "Admin");
        var store = new InMemoryMethodPermissionStore(new[] { rule });

        var result = store.GetRules(new MethodKey("TypeA", "Method"));

        result.Should().ContainSingle().Which.Should().Be(rule);
    }

    [Fact]
    public void GetRules_KeyMissing_ReturnsEmptyCollection()
    {
        var store = new InMemoryMethodPermissionStore(Array.Empty<MethodPermissionRule>());

        var result = store.GetRules(new MethodKey("TypeA", "Method"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void MultipleRulesForSameMethod_AreGroupedTogether()
    {
        var rules = new[]
        {
            CreateRule("TypeA", "Method", RoleRuleKind.AnyOf, "Admin"),
            CreateRule("TypeA", "Method", RoleRuleKind.AllOf, "Admin", "Sales"),
            CreateRule("TypeB", "Other", RoleRuleKind.AnyOf, "User")
        };

        var store = new InMemoryMethodPermissionStore(rules);

        var result = store.GetRules(new MethodKey("TypeA", "Method"));

        result.Should().HaveCount(2);
    }

    private static MethodPermissionRule CreateRule(string type, string method, RoleRuleKind kind, params string[] roles) =>
        new(type, method, kind, roles);
}
