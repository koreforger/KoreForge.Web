using System.Net.Http.Headers;
using KF.RestApi.External.Sample.Options;
using Microsoft.Extensions.Options;

namespace KF.RestApi.External.Sample.Handlers;

/// <summary>
/// Applies the configured authentication scheme to outbound HTTP requests.
/// </summary>
internal sealed class ApiAuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<SampleAuthenticationOptions> _options;

    public ApiAuthenticationDelegatingHandler(IOptionsMonitor<SampleAuthenticationOptions> options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authOptions = _options.Get(SampleConstants.ApiName);
        if (!string.IsNullOrWhiteSpace(authOptions.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                authOptions.Scheme,
                authOptions.BearerToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
