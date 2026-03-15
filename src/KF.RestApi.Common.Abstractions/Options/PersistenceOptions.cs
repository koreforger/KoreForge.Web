using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.Common.Abstractions.Options;

/// <summary>
/// Describes how persistence components should connect to the backing database.
/// </summary>
public sealed class PersistenceOptions
{
    /// <summary>
    /// Name of the connection string the DbContext should use.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string ConnectionStringName { get; init; } = "ApiGateway";

    /// <summary>
    /// Allows falling back to an in-memory provider for development/test scenarios.
    /// </summary>
    public bool AllowInMemoryFallback { get; init; } = true;
}
