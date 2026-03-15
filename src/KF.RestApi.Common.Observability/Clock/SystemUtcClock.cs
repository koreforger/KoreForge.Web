using KF.RestApi.Common.Abstractions.Time;

namespace KF.RestApi.Common.Observability.Clock;

/// <summary>
/// Production implementation of <see cref="IUtcClock"/>.
/// </summary>
public sealed class SystemUtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
