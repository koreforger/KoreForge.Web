using KoreForge.RestApi.Client.Sample.Services;
using KoreForge.RestApi.Common.Abstractions.DependencyInjection;
using KoreForge.RestApi.Common.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KoreForge.RestApi.Client.Sample.DependencyInjection;

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
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"KoreForge.RestApi.Client.{SampleClientConstants.ApiName}/1.0");
        });

        return services;
    }
}
