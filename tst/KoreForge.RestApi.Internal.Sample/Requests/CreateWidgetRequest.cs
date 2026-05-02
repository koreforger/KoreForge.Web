using System.ComponentModel.DataAnnotations;

namespace KoreForge.RestApi.Internal.Sample.Requests;

/// <summary>
/// HTTP payload accepted by the Internal host.
/// </summary>
public sealed class CreateWidgetRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public string? CallerSystem { get; init; }
}
