using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.External.Sample.Options;

/// <summary>
/// Authentication settings applied to outbound HTTP calls.
/// </summary>
internal sealed class SampleAuthenticationOptions
{
    /// <summary>
    /// Authorization scheme communicated to the upstream provider (e.g. Bearer, Basic).
    /// </summary>
    [MinLength(3)]
    public string Scheme { get; init; } = "Bearer";

    /// <summary>
    /// Token or secret forwarded to the upstream provider.
    /// </summary>
    [Required]
    [MinLength(10)]
    public string? BearerToken { get; init; }
}
