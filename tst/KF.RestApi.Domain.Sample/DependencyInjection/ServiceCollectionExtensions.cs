using KF.RestApi.Domain.Sample.Options;
using KF.RestApi.Domain.Sample.Services;
using KF.RestApi.External.Sample;
using KF.RestApi.External.Sample.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KF.RestApi.Domain.Sample.DependencyInjection;

/// <summary>
/// Registers Domain services plus the dependent External client.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleDomain(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSampleExternal(configuration);

        services.AddOptions<SampleDomainOptions>(SampleConstants.ApiName)
            .Bind(configuration.GetSection(SampleDomainOptions.ConfigurationSection))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ISampleService, SampleService>();
        return services;
    }
}
