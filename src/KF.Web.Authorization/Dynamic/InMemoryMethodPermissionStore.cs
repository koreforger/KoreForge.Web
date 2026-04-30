namespace KF.Web.Authorization.Dynamic;

/// <summary>
/// Simple dictionary-backed implementation of <see cref="IMethodPermissionStore"/>.
/// </summary>
public sealed class InMemoryMethodPermissionStore : IMethodPermissionStore
{
    private readonly IReadOnlyDictionary<MethodKey, IReadOnlyCollection<MethodPermissionRule>> _rulesByKey;

    /// <summary>
    /// Creates the store by grouping supplied rules by method key.
    /// </summary>
    /// <param name="rules">Rules to index.</param>
    public InMemoryMethodPermissionStore(IEnumerable<MethodPermissionRule> rules)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        _rulesByKey = rules
            .GroupBy(r => new MethodKey(r.TypeFullName, r.MethodName))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<MethodPermissionRule>)g.ToArray());
    }

    /// <inheritdoc />
    public IReadOnlyCollection<MethodPermissionRule> GetRules(MethodKey methodKey)
    {
        if (_rulesByKey.TryGetValue(methodKey, out var rules))
        {
            return rules;
        }

        return Array.Empty<MethodPermissionRule>();
    }
}
