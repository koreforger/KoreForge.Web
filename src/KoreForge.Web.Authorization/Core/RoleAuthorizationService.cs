using System.Security.Claims;

namespace KoreForge.Web.Authorization.Core;

/// <summary>
/// Default implementation of <see cref="IRoleAuthorizationService"/> that reads role claims from <see cref="ClaimTypes.Role"/>.
/// </summary>
public sealed class RoleAuthorizationService : IRoleAuthorizationService
{
    /// <inheritdoc />
    public bool IsAuthorized(
        ClaimsPrincipal user,
        RoleRuleKind rule,
        IReadOnlyCollection<string> roles)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (roles is null)
        {
            throw new ArgumentNullException(nameof(roles));
        }

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
            RoleRuleKind.AnyOf => requiredRoles.Any(userRoles.Contains),
            RoleRuleKind.AllOf => requiredRoles.All(userRoles.Contains),
            RoleRuleKind.NotAnyOf => !requiredRoles.Any(userRoles.Contains),
            RoleRuleKind.NotAllOf => !requiredRoles.All(userRoles.Contains),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unsupported rule.")
        };
    }
}
