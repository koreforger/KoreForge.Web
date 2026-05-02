namespace KoreForge.Web.Authorization.Dynamic;

/// <summary>
/// Identifies a controller action by its declaring type and method name.
/// </summary>
public readonly struct MethodKey : IEquatable<MethodKey>
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
    /// Creates a new <see cref="MethodKey"/>.
    /// </summary>
    /// <param name="typeFullName">Fully qualified type name.</param>
    /// <param name="methodName">Action method name.</param>
    public MethodKey(string typeFullName, string methodName)
    {
        TypeFullName = typeFullName ?? throw new ArgumentNullException(nameof(typeFullName));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <inheritdoc />
    public bool Equals(MethodKey other) =>
        string.Equals(TypeFullName, other.TypeFullName, StringComparison.Ordinal)
        && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MethodKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(TypeFullName),
            StringComparer.Ordinal.GetHashCode(MethodName));

    /// <summary>
    /// Returns a debug-friendly representation of the key.
    /// </summary>
    public override string ToString() => $"{TypeFullName}::{MethodName}";
}
