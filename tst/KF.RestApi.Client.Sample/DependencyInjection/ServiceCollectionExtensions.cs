using KF.RestApi.Client.Sample.Services;
using KF.RestApi.Common.Abstractions.DependencyInjection;
using KF.RestApi.Common.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KF.RestApi.Client.Sample.DependencyInjection;

/// <summary>
/// Registers the SDK + typed HttpClient.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddInternalApiOptions(configuration, SampleClientConstants.ApiName, sectionPrefix: "InternalApis");

        services.AddHttpClient<ISampleClient, SampleClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<InternalApiOptions>>()
                .Get(SampleClientConstants.ApiName);

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"KF.RestApi.Client.{SampleClientConstants.ApiName}/1.0");
        });

        return services;
    }
}
