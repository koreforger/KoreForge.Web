using System.ComponentModel.DataAnnotations;

namespace KoreForge.RestApi.Client.Sample.Models;

/// <summary>
/// Client-friendly command for creating widgets.
/// </summary>
public sealed class CreateWidgetRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }
}
