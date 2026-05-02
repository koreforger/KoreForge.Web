using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KoreForge.RestApi.External.Sample.Models;

/// <summary>
/// Sample payload sent to the upstream provider.
/// </summary>
internal sealed class CreateWidgetRequest
{
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
