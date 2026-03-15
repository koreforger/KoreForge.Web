using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.Domain.Sample.Options;

/// <summary>
/// Configures Domain-layer defaults such as auditing, schema, and table layout.
/// </summary>
internal sealed class SampleDomainOptions
{
    public const string ConfigurationSection = "Domains:" + KF.RestApi.External.Sample.SampleConstants.ApiName;

    [Required]
    [MinLength(3)]
    public string DbSchema { get; init; } = "Audit";

    [Required]
    [MinLength(3)]
    public string TableMode { get; init; } = "Single";

    public bool EnableAuditing { get; init; } = bool.Parse("True");
}
