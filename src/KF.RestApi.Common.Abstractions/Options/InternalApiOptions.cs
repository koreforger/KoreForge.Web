using System.ComponentModel.DataAnnotations;

namespace KF.RestApi.Common.Abstractions.Options;

/// <summary>
/// Options governing access to internal HTTP endpoints exposed by Host.Internal.
/// </summary>
public sealed class InternalApiOptions
{
    /// <summary>
    /// Base URL of the internal HTTP gateway surface.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Timeout limit communicated to <see cref="HttpClient"/> usage.
    /// </summary>
    [Range(1, 600)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Derived timeout value as <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
