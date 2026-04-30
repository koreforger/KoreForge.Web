namespace KF.Web.Authorization.Core;

/// <summary>
/// Describes how required roles should be evaluated for a request.
/// </summary>
public enum RoleRuleKind
{
    /// <summary>
    /// The user must be a member of at least one required role.
    /// </summary>
    AnyOf,

    /// <summary>
    /// The user must be a member of all required roles.
    /// </summary>
    AllOf,

    /// <summary>
    /// The user must not belong to any of the listed roles.
    /// </summary>
    NotAnyOf,

    /// <summary>
    /// The user must not possess the entire listed role set simultaneously.
    /// </summary>
    NotAllOf
}
