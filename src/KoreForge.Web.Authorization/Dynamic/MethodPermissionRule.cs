using KoreForge.Web.Authorization.Core;

namespace KoreForge.Web.Authorization.Dynamic;

/// <summary>
/// Represents the role and condition requirements for a specific controller action.
/// </summary>
public sealed class MethodPermissionRule
{
    /// <summary>
    /// Gets the fully qualified controller type name.
    /// </summary>
    public string TypeFullName { get; }

    /// <summary>
    /// Gets the action method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the semantic rule applied to the <see cref="Roles"/> collection.
    /// </summary>
    public RoleRuleKind RuleKind { get; }

    /// <summary>
    /// Gets the normalized role list.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets the optional delegate evaluated after the role check succeeds.
    /// </summary>
    public PermissionConditionDelegate? Condition { get; }

    /// <summary>
    /// Creates a new rule definition.
    /// </summary>
    /// <param name="typeFullName">Fully qualified controller type name.</param>
    /// <param name="methodName">Action method name.</param>
    /// <param name="ruleKind">Role semantics to apply.</param>
    /// <param name="roles">Role list evaluated by the semantics.</param>
    /// <param name="condition">Optional condition delegate.</param>
    public MethodPermissionRule(
        string typeFullName,
        string methodName,
        RoleRuleKind ruleKind,
        IEnumerable<string> roles,
        PermissionConditionDelegate? condition = null)
    {
        if (string.IsNullOrWhiteSpace(typeFullName))
        {
            throw new ArgumentException("Type full name is required.", nameof(typeFullName));
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("Method name is required.", nameof(methodName));
        }

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
