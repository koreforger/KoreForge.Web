namespace KoreForge.RestApi.Common.Abstractions.Time;

/// <summary>
/// Provides the current UTC time. Abstracted for deterministic testing.
/// </summary>
public interface IUtcClock
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
