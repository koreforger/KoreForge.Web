namespace KF.Web.Authorization.Dynamic;

/// <summary>
/// Provides access to the configured <see cref="MethodPermissionRule"/> entries for controller actions.
/// </summary>
public interface IMethodPermissionStore
{
    /// <summary>
    /// Gets the rules associated with the provided controller/method key.
    /// </summary>
    /// <param name="methodKey">Represents the controller action identifier.</param>
    /// <returns>A collection of rules (possibly empty).</returns>
    IReadOnlyCollection<MethodPermissionRule> GetRules(MethodKey methodKey);
}
