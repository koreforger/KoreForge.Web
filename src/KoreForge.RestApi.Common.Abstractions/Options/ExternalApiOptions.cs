using System.ComponentModel.DataAnnotations;

namespace KoreForge.RestApi.Common.Abstractions.Options;

/// <summary>
/// Represents typed configuration for outbound provider calls.
/// </summary>
public sealed class ExternalApiOptions
{
    /// <summary>
    /// Base URL of the upstream provider.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Timeout threshold, expressed in seconds.
    /// </summary>
    [Range(1, 600)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// HTTP user-agent string attached to outbound requests.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string UserAgent { get; init; } = "ApiGatewayFramework/1.0";

    /// <summary>
    /// Convenience property that exposes <see cref="TimeoutSeconds"/> as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
