using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.Common.Persistence.Options;

/// <summary>
/// Configures how audit records are persisted (schema, table naming, mode).
/// </summary>
public sealed class AuditStoreOptions
{
    public const string ConfigurationSection = "Persistence:Audit";

    /// <summary>
    /// Database schema used for audit tables.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string Schema { get; init; } = "Audit";

    /// <summary>
    /// Table naming template. Supports the placeholder {ApiName}.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string TableNameTemplate { get; init; } = "Api_{ApiName}_Calls";

    /// <summary>
    /// Table naming template for request payloads when using split tables.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string RequestTableNameTemplate { get; init; } = "Api_{ApiName}_Requests";

    /// <summary>
    /// Table naming template for response payloads when using split tables.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string ResponseTableNameTemplate { get; init; } = "Api_{ApiName}_Responses";

    /// <summary>
    /// Default API name used when computing the table name from the template.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string DefaultApiName { get; init; } = "Sample";

    /// <summary>
    /// Whether audit rows should be written into a single table or split per request/response.
    /// </summary>
    [Required]
    [RegularExpression("Single|Split", ErrorMessage = "TableMode must be either 'Single' or 'Split'.")]
    public string TableMode { get; init; } = "Single";

    /// <summary>
    /// Optional retention window (in days) for cleanup jobs.
    /// </summary>
    [Range(1, 3650)]
    public int? RetentionDays { get; init; }

    internal static AuditStoreOptions Default { get; } = new();
}