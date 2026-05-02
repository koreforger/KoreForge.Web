using System.Globalization;
using KoreForge.RestApi.Common.Persistence.Entities;
using KoreForge.RestApi.Common.Persistence.Options;
using KoreForge.RestApi.Domain.Sample.Options;
using KoreForge.RestApi.External.Sample;

namespace KoreForge.RestApi.Domain.Sample.Auditing;

/// <summary>
/// Builds audit rows aligned with the shared persistence model.
/// </summary>
internal static class SampleAuditRecordFactory
{
    public static ApiCallAudit Create(
        string operation,
        string httpMethod,
        string? callerSystem,
        int statusCode,
        string? requestPath,
        string? correlationId,
        string requestPayload,
        string? responsePayload,
        DateTimeOffset requestTimestamp,
        DateTimeOffset responseTimestamp,
        SampleDomainOptions options,
        AuditRedactionOptions redaction)
    {
        var redactedRequest = PayloadRedactor.Redact(requestPayload, redaction);
        var redactedResponse = PayloadRedactor.Redact(responsePayload, redaction);

        return new ApiCallAudit
        {
            ApiName = SampleConstants.ApiName,
            Operation = operation,
            Direction = options.TableMode,
            CallerSystem = callerSystem ?? options.DbSchema,
            StatusCode = statusCode,
            ErrorCode = statusCode >= 400 ? statusCode.ToString(CultureInfo.InvariantCulture) : null,
            ErrorMessage = statusCode >= 400 ? "Provider returned an error." : null,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            RequestPayload = redactedRequest,
            ResponsePayload = redactedResponse,
            RequestTimestampUtc = requestTimestamp,
            ResponseTimestampUtc = responseTimestamp,
            DurationMs = (long)(responseTimestamp - requestTimestamp).TotalMilliseconds,
            CorrelationId = correlationId
        };
    }
}
