using System.Diagnostics;

namespace KF.RestApi.Common.Observability.Tracing;

internal sealed class ActivityTraceSpan : ITraceSpan
{
    private readonly Activity _activity;

    public ActivityTraceSpan(Activity activity)
    {
        _activity = activity;
    }

    public string Name => _activity.OperationName;

    public string CorrelationId => _activity.TraceId.ToString();

    public void Dispose()
    {
        _activity.Stop();
    }
}
