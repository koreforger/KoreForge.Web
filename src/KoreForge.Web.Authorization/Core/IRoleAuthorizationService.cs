using System.Security.Claims;

namespace KoreForge.Web.Authorization.Core;

/// <summary>
/// Provides role-semantics evaluation for attribute and dynamic authorization flows.
/// </summary>
public interface IRoleAuthorizationService
{
    /// <summary>
    /// Determines whether a user satisfies the supplied role rule.
    /// </summary>
    /// <param name="user">The claims principal to inspect.</param>
    /// <param name="rule">The semantic rule describing how roles should be matched.</param>
    /// <param name="roles">The set of roles referenced by the rule.</param>
    /// <returns><c>true</c> when the rule succeeds; otherwise, <c>false</c>.</returns>
    bool IsAuthorized(
        ClaimsPrincipal user,
        RoleRuleKind rule,
        IReadOnlyCollection<string> roles);
}
