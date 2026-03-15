using System.Diagnostics;

namespace KF.RestApi.Common.Observability.Tracing;

/// <summary>
/// Default tracer implementation backed by <see cref="ActivitySource"/>.
/// </summary>
public sealed class ActivityTracer : ITracer, IDisposable
{
    public const string ActivitySourceName = "KoreForge.Web";

    private readonly ActivitySource _activitySource = new(ActivitySourceName);
    private bool _disposed;

    public ITraceSpan StartSpan(string name)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ActivityTracer));
        }

        var activity = _activitySource.StartActivity(name, ActivityKind.Internal);
        activity ??= new Activity(name);
        activity.Start();

        return new ActivityTraceSpan(activity);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _activitySource.Dispose();
        _disposed = true;
    }
}
