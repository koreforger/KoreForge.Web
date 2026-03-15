using System.Text.Json.Serialization;

namespace KF.RestApi.Client.Sample.Models;

/// <summary>
/// Minimal representation of RFC 9457 ProblemDetails returned by Internal APIs.
/// </summary>
public sealed class ProblemDetailsPayload
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object?> Extensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
