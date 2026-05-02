using System.Text.Json.Serialization;

namespace KoreForge.RestApi.External.Sample.Models;

/// <summary>
/// Health payload echoed by the upstream provider.
/// </summary>
internal sealed class PingResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; init; }
}
