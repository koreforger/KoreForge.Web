using KoreForge.RestApi.Common.Abstractions.Time;
using KoreForge.RestApi.Common.Observability.Tracing;
using Microsoft.Extensions.Logging;

namespace KoreForge.RestApi.External.Sample.Handlers;

/// <summary>
/// Captures request/response payloads and propagates a correlation identifier.
/// </summary>
internal sealed class ApiCallHooksDelegatingHandler : DelegatingHandler
{
    private readonly IUtcClock _clock;
    private readonly ILogger<ApiCallHooksDelegatingHandler> _logger;
    private readonly ITracer _tracer;

    public ApiCallHooksDelegatingHandler(
        IUtcClock clock,
        ILogger<ApiCallHooksDelegatingHandler> logger,
        ITracer tracer)
    {
        _clock = clock;
        _logger = logger;
        _tracer = tracer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var span = _tracer.StartSpan($"KoreForge.RestApi.External.{SampleConstants.ApiName}.{request.Method.Method}");
        var startedAt = _clock.UtcNow;
        var requestPayload = await ReadPayloadAsync(request.Content, cancellationToken).ConfigureAwait(false);
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", span.CorrelationId);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responsePayload = await ReadPayloadAsync(response.Content, cancellationToken).ConfigureAwait(false);
            var durationMs = (_clock.UtcNow - startedAt).TotalMilliseconds;

            _logger.LogInformation(
                "External call {ApiName} {Method} {Path} completed with status {Status} in {Duration} ms. CorrelationId={CorrelationId}. Request={RequestPayload} Response={ResponsePayload}",
                SampleConstants.ApiName,
                request.Method,
                request.RequestUri?.PathAndQuery,
                (int)response.StatusCode,
                durationMs,
                span.CorrelationId,
                requestPayload,
                responsePayload);

            response.Headers.TryAddWithoutValidation("X-Correlation-Id", span.CorrelationId);
            return response;
        }
        catch (Exception ex) when (LogFailure(ex, request, requestPayload, span.CorrelationId, startedAt))
        {
            throw;
        }
    }

    private bool LogFailure(
        Exception exception,
        HttpRequestMessage request,
        string? requestPayload,
        string correlationId,
        DateTimeOffset startedAt)
    {
        var durationMs = (_clock.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogError(
            exception,
            "External call {ApiName} {Method} {Path} failed after {Duration} ms. CorrelationId={CorrelationId}. Request={RequestPayload}",
            SampleConstants.ApiName,
            request.Method,
            request.RequestUri?.PathAndQuery,
            durationMs,
            correlationId,
            requestPayload);

        return false;
    }

    private static async Task<string?> ReadPayloadAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return null;
        }

        return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
