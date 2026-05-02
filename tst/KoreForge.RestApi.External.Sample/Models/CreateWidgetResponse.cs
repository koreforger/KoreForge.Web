using System.Text.Json.Serialization;

namespace KoreForge.RestApi.External.Sample.Models;

/// <summary>
/// Sample response from a create widget call.
/// </summary>
internal sealed class CreateWidgetResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
