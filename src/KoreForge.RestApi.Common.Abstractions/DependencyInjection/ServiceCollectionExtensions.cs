using KoreForge.RestApi.Common.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KoreForge.RestApi.Common.Abstractions.DependencyInjection;

/// <summary>
/// Helper extensions for registering strongly-typed options with validation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers validated options for a specific external API.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <param name="apiName">Name of the API being configured.</param>
    /// <param name="sectionPrefix">Configuration prefix, defaults to <c>ExternalApis</c>.</param>
    public static OptionsBuilder<ExternalApiOptions> AddExternalApiOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiName,
        string? sectionPrefix = "ExternalApis")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);

        var section = configuration.GetSection(BuildSectionPath(sectionPrefix, apiName));
        return services.AddValidatedOptions<ExternalApiOptions>(apiName, section);
    }

    /// <summary>
    /// Registers validated options for a specific internal API consumer.
    /// </summary>
    public static OptionsBuilder<InternalApiOptions> AddInternalApiOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiName,
        string? sectionPrefix = "InternalApis")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);

        var section = configuration.GetSection(BuildSectionPath(sectionPrefix, apiName));
        return services.AddValidatedOptions<InternalApiOptions>(apiName, section);
    }

    /// <summary>
    /// Registers persistence-wide options.
    /// </summary>
    public static OptionsBuilder<PersistenceOptions> AddPersistenceOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionPath = "Persistence")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath ?? "Persistence");
        return services.AddValidatedOptions<PersistenceOptions>(nameof(PersistenceOptions), section);
    }

    private static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        string? name,
        IConfigurationSection section)
        where TOptions : class, new()
    {
        var builder = name is null
            ? services.AddOptions<TOptions>()
            : services.AddOptions<TOptions>(name);

        builder.Bind(section);
        builder.ValidateDataAnnotations();
        builder.Validate(
            static options => options is not null,
            "Options binding failed. The configuration section did not contain any values.");

        return builder;
    }

    private static string BuildSectionPath(string? prefix, string apiName)
    {
        return string.IsNullOrWhiteSpace(prefix)
            ? apiName
            : string.Create(prefix.Length + apiName.Length + 1, (prefix, apiName), static (span, state) =>
            {
                var (p, a) = state;
                p.AsSpan().CopyTo(span);
                span[p.Length] = ':';
                a.AsSpan().CopyTo(span[(p.Length + 1)..]);
            });
    }
}
