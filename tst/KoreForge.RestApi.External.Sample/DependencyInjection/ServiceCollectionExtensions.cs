using KoreForge.RestApi.Common.Abstractions.DependencyInjection;
using KoreForge.RestApi.Common.Abstractions.Options;
using KoreForge.RestApi.External.Sample.Handlers;
using KoreForge.RestApi.External.Sample.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace KoreForge.RestApi.External.Sample.DependencyInjection;

/// <summary>
/// Registers the transport-only Refit client for this provider.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleExternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddExternalApiOptions(
            configuration,
            SampleConstants.ApiName,
            sectionPrefix: "Apis");

        services.AddOptions<SampleAuthenticationOptions>(SampleConstants.ApiName)
            .Bind(configuration.GetSection(SampleConstants.ConfigurationSection))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<ApiAuthenticationDelegatingHandler>();
        services.AddTransient<ApiCallHooksDelegatingHandler>();

        services
            .AddRefitClient<ISampleApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp
                    .GetRequiredService<IOptionsMonitor<ExternalApiOptions>>()
                    .Get(SampleConstants.ApiName);

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .AddHttpMessageHandler<ApiAuthenticationDelegatingHandler>()
            .AddHttpMessageHandler<ApiCallHooksDelegatingHandler>();

        return services;
    }
}
