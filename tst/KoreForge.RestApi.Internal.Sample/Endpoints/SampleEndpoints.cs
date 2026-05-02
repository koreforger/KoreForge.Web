using KoreForge.RestApi.Domain.Sample.Exceptions;
using KoreForge.RestApi.Domain.Sample.Models;
using KoreForge.RestApi.Domain.Sample.Services;
using KoreForge.RestApi.Internal.Sample.Requests;
using KoreForge.RestApi.Internal.Sample.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace KoreForge.RestApi.Internal.Sample.Endpoints;

/// <summary>
/// Minimal API endpoints wired into KoreForge.RestApi.Host.Internal.
/// </summary>
public static class SampleEndpoints
{
    public static IEndpointRouteBuilder MapSampleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/apis/api-module-seed")
            .WithTags("Sample");

        group.MapPost("/widgets", HandleCreateWidgetAsync)
            .WithName("Sample_CreateWidget")
            .Produces<CreateWidgetResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<IResult> HandleCreateWidgetAsync(
        CreateWidgetRequest request,
        ISampleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateWidgetCommand
            {
                Name = request.Name,
                Description = request.Description,
                CallerSystem = request.CallerSystem
            };

            var result = await service.CreateWidgetAsync(command, cancellationToken).ConfigureAwait(false);
            var response = CreateWidgetResponse.From(result);

            return Results.Created($"/apis/api-module-seed/widgets/{result.Id}", response);
        }
        catch (SampleExternalException ex)
        {
            var problem = new ProblemDetails
            {
                Title = "Upstream provider rejected the request.",
                Detail = ex.Message,
                Status = StatusCodes.Status502BadGateway,
                Extensions =
                {
                    ["correlationId"] = ex.Data["CorrelationId"] ?? string.Empty,
                    ["providerStatusCode"] = ex.Data["StatusCode"] ?? string.Empty,
                    ["operation"] = ex.Data["operation"] ?? "CreateWidget"
                }
            };

            return Results.Problem(problem);
        }
    }
}
