using System.Net.Http.Json;
using KF.RestApi.Client.Sample.Exceptions;
using KF.RestApi.Client.Sample.Models;

namespace KF.RestApi.Client.Sample.Services;

/// <summary>
/// HTTP client that targets KF.RestApi.Host.Internal endpoints for this module.
/// </summary>
internal sealed class SampleClient : ISampleClient
{
    private readonly HttpClient _httpClient;

    public SampleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateWidgetResult> CreateWidgetAsync(
        CreateWidgetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync(
            requestUri: SampleClientConstants.EndpointPrefix + "/widgets",
            value: request,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<CreateWidgetResult>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (payload is null)
            {
                throw new InvalidOperationException("Internal API returned an empty payload.");
            }

            return payload;
        }

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetailsPayload>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        throw InternalApiException.FromProblem(response.StatusCode, problem);
    }
}
