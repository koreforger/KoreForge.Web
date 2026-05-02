using KoreForge.RestApi.External.Sample.Models;
using Refit;

namespace KoreForge.RestApi.External.Sample;

/// <summary>
/// Transport-only Refit contract generated from the upstream OpenAPI description.
/// </summary>
internal interface ISampleApi
{
    /// <summary>
    /// Retrieves the upstream health status.
    /// </summary>
    /// <remarks>Returns 200 when the provider is reachable.</remarks>
    [Get("/ping")]
    Task<ApiResponse<PingResponse>> GetPingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new widget resource.
    /// </summary>
    /// <response code="201">Widget created successfully.</response>
    /// <response code="400">Payload validation failed.</response>
    /// <response code="500">Provider error.</response>
    [Post("/widgets")]
    Task<ApiResponse<CreateWidgetResponse>> CreateWidgetAsync(
        [Body] CreateWidgetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a widget by identifier.
    /// </summary>
    /// <response code="200">Widget found.</response>
    /// <response code="404">Widget is missing upstream.</response>
    [Get("/widgets/{widgetId}")]
    Task<ApiResponse<CreateWidgetResponse>> GetWidgetAsync(
        string widgetId,
        CancellationToken cancellationToken = default);
}
