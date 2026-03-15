using System.Security.Claims;
using FluentAssertions;
using KF.Web.Authorization.Core;
using KF.Web.Authorization.Tests.TestHelpers;

namespace KF.Web.Authorization.Tests.Core;

public class RoleAuthorizationServiceTests
{
    private readonly RoleAuthorizationService _sut = new();

    [Fact]
    public void AnyOf_UserHasOneRequiredRole_ReturnsTrue()
    {
        var user = ClaimsPrincipalFactory.Create("Admin");
        _sut.IsAuthorized(user, RoleRuleKind.AnyOf, new[] { "Admin", "Support" }).Should().BeTrue();
    }

    [Fact]
    public void AnyOf_UserHasNoneOfRequiredRoles_ReturnsFalse()
    {
        var user = ClaimsPrincipalFactory.Create("User");
        _sut.IsAuthorized(user, RoleRuleKind.AnyOf, new[] { "Admin" }).Should().BeFalse();
    }

    [Fact]
    public void AllOf_UserHasAllRoles_ReturnsTrue()
    {
        var user = ClaimsPrincipalFactory.Create("Admin", "Supervisor");
        _sut.IsAuthorized(user, RoleRuleKind.AllOf, new[] { "Admin", "Supervisor" }).Should().BeTrue();
    }

    [Fact]
    public void AllOf_UserMissingAtLeastOneRole_ReturnsFalse()
    {
        var user = ClaimsPrincipalFactory.Create("Admin");
        _sut.IsAuthorized(user, RoleRuleKind.AllOf, new[] { "Admin", "Supervisor" }).Should().BeFalse();
    }

    [Fact]
    public void NotAnyOf_UserHasNoneRoles_ReturnsTrue()
    {
        var user = ClaimsPrincipalFactory.Create("User");
        _sut.IsAuthorized(user, RoleRuleKind.NotAnyOf, new[] { "Suspended" }).Should().BeTrue();
    }

    [Fact]
    public void NotAnyOf_UserHasForbiddenRole_ReturnsFalse()
    {
        var user = ClaimsPrincipalFactory.Create("Suspended");
        _sut.IsAuthorized(user, RoleRuleKind.NotAnyOf, new[] { "Suspended" }).Should().BeFalse();
    }

    [Fact]
    public void NotAllOf_UserHasAllRoles_ReturnsFalse()
    {
        var user = ClaimsPrincipalFactory.Create("Trader", "Auditor");
        _sut.IsAuthorized(user, RoleRuleKind.NotAllOf, new[] { "Trader", "Auditor" }).Should().BeFalse();
    }

    [Fact]
    public void NotAllOf_UserMissingOneRole_ReturnsTrue()
    {
        var user = ClaimsPrincipalFactory.Create("Trader");
        _sut.IsAuthorized(user, RoleRuleKind.NotAllOf, new[] { "Trader", "Auditor" }).Should().BeTrue();
    }

    [Fact]
    public void EmptyRoles_ReturnsTrueRegardlessOfUserRoles()
    {
        var user = ClaimsPrincipalFactory.Create("Trader");
        _sut.IsAuthorized(user, RoleRuleKind.AnyOf, Array.Empty<string>()).Should().BeTrue();
    }

    [Fact]
    public void RoleComparison_IsCaseInsensitive()
    {
        var user = ClaimsPrincipalFactory.Create("admin");
        _sut.IsAuthorized(user, RoleRuleKind.AnyOf, new[] { "ADMIN" }).Should().BeTrue();
    }

    [Fact]
    public void NullUser_ThrowsArgumentNullException()
    {
        var act = () => _sut.IsAuthorized(null!, RoleRuleKind.AnyOf, Array.Empty<string>());
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("user");
    }

    [Fact]
    public void NullRoles_ThrowsArgumentNullException()
    {
        var user = ClaimsPrincipalFactory.Create();
        var act = () => _sut.IsAuthorized(user, RoleRuleKind.AnyOf, null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("roles");
    }
}
