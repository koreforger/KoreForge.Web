namespace KF.RestApi.Domain.Sample.Models;

/// <summary>
/// Domain-level command that orchestrates a create widget flow.
/// </summary>
public sealed class CreateWidgetCommand
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? CallerSystem { get; init; }
}
