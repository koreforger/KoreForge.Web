using KoreForge.Web.Authorization.Core;
using Microsoft.AspNetCore.Mvc;

namespace KoreForge.Web.Authorization.Core.Mvc;

/// <summary>
/// Decorates controllers or actions with role-based authorization semantics and optional conditions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RolesAuthorizeAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new <see cref="RolesAuthorizeAttribute"/> instance.
    /// </summary>
    /// <param name="rule">The role semantics applied to the supplied roles.</param>
    /// <param name="roles">Roles evaluated for the request.</param>
    /// <param name="conditionType">Optional condition type implementing <see cref="IContextAuthorizationCondition"/>.</param>
    public RolesAuthorizeAttribute(
        RoleRuleKind rule,
        string[]? roles,
        Type? conditionType = null)
        : base(typeof(RolesAuthorizationFilter))
    {
        Arguments = new object[]
        {
            rule,
            roles ?? Array.Empty<string>(),
            new ConditionTypeHolder(conditionType)
        };
    }

    /// <summary>
    /// Lightweight wrapper used to preserve optional condition type metadata.
    /// </summary>
    public sealed class ConditionTypeHolder
    {
        /// <summary>
        /// Creates a new holder instance.
        /// </summary>
        /// <param name="value">Optional condition type.</param>
        public ConditionTypeHolder(Type? value) => Value = value;

        /// <summary>
        /// Gets the stored condition type.
        /// </summary>
        public Type? Value { get; }
    }
}
