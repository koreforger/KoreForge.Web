using System.Text.Json;
using KoreForge.RestApi.Common.Abstractions.Time;
using KoreForge.RestApi.Common.Observability.Tracing;
using KoreForge.RestApi.Common.Persistence.Repositories;
using KoreForge.RestApi.Domain.Sample.Auditing;
using KoreForge.RestApi.Domain.Sample.Exceptions;
using KoreForge.RestApi.Domain.Sample.Models;
using KoreForge.RestApi.Domain.Sample.Options;
using KoreForge.RestApi.External.Sample;
using KoreForge.RestApi.External.Sample.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using KoreForge.RestApi.Common.Persistence.Options;

namespace KoreForge.RestApi.Domain.Sample.Services;

/// <summary>
/// Contains the orchestration logic for this API module.
/// </summary>
internal sealed class SampleService : ISampleService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ISampleApi _api;
    private readonly IApiAuditRepository _auditRepository;
    private readonly IUtcClock _clock;
    private readonly ILogger<SampleService> _logger;
    private readonly IOptionsMonitor<SampleDomainOptions> _options;
    private readonly ITracer _tracer;
    private readonly AuditRedactionOptions _redactionOptions;

    public SampleService(
        ISampleApi api,
        IApiAuditRepository auditRepository,
        IUtcClock clock,
        ILogger<SampleService> logger,
        IOptionsMonitor<SampleDomainOptions> options,
        ITracer tracer,
        IOptions<AuditRedactionOptions> redactionOptions)
    {
        _api = api;
        _auditRepository = auditRepository;
        _clock = clock;
        _logger = logger;
        _options = options;
        _tracer = tracer;
        _redactionOptions = redactionOptions.Value;
    }

    public async Task<WidgetResult> CreateWidgetAsync(CreateWidgetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var span = _tracer.StartSpan("KoreForge.RestApi.Domain.Sample.CreateWidget");
        var startedAt = _clock.UtcNow;
        var request = new CreateWidgetRequest
        {
            Name = command.Name,
            Description = command.Description
        };

        var response = await _api.CreateWidgetAsync(request, cancellationToken).ConfigureAwait(false);
        var requestJson = Serialize(request) ?? string.Empty;
        var responseJson = Serialize(response.Content) ?? response.Error?.Content;

        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            throw SampleExternalException.FromResponse(
                "CreateWidget",
                response,
                requestJson,
                responseJson,
                span.CorrelationId);
        }

        var requestMessage = response.RequestMessage ?? response.Error?.RequestMessage;

        var httpMethod = requestMessage?.Method.Method ?? "POST";
        var requestPath = requestMessage?.RequestUri?.AbsolutePath ?? "/widgets";

        await TryAuditAsync(
            operation: "CreateWidget",
            httpMethod: httpMethod,
            requestPath: requestPath,
            statusCode: (int)response.StatusCode,
            callerSystem: command.CallerSystem,
            requestJson,
            responseJson,
            span.CorrelationId,
            startedAt,
            _clock.UtcNow,
            _redactionOptions,
            cancellationToken).ConfigureAwait(false);

        return new WidgetResult(
            response.Content.Id,
            response.Content.Name,
            response.Content.Status,
            span.CorrelationId);
    }

    private async Task TryAuditAsync(
        string operation,
        string httpMethod,
        string? requestPath,
        int statusCode,
        string? callerSystem,
        string requestJson,
        string? responseJson,
        string? correlationId,
        DateTimeOffset requestTimestamp,
        DateTimeOffset responseTimestamp,
        AuditRedactionOptions redactionOptions,
        CancellationToken cancellationToken)
    {
        var options = _options.Get(SampleConstants.ApiName);
        if (!options.EnableAuditing)
        {
            return;
        }

        var audit = SampleAuditRecordFactory.Create(
            operation,
            httpMethod,
            callerSystem,
            statusCode,
            requestPath,
            correlationId,
            requestJson,
            responseJson,
            requestTimestamp,
            responseTimestamp,
            options,
            redactionOptions);

        try
        {
            await _auditRepository.SaveAsync(audit, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist audit entry for {ApiName} operation {Operation}.",
                SampleConstants.ApiName,
                operation);
        }
    }

    private static string? Serialize<T>(T? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, SerializerOptions);
    }
}
