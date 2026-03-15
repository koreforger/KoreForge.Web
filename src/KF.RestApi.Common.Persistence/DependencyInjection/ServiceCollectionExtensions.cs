using KF.RestApi.Common.Abstractions.DependencyInjection;
using KF.RestApi.Common.Abstractions.Options;
using KF.RestApi.Common.Persistence.Options;
using KF.RestApi.Common.Persistence.Repositories;
using KF.RestApi.Common.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KF.RestApi.Common.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string? connectionStringName = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddPersistenceOptions(configuration);
        services.AddOptions<AuditStoreOptions>()
            .Bind(configuration.GetSection(AuditStoreOptions.ConfigurationSection))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AuditRedactionOptions>()
            .Bind(configuration.GetSection(AuditRedactionOptions.ConfigurationSection))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var options = configuration
            .GetSection("Persistence")
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        connectionStringName ??= options.ConnectionStringName;
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!options.AllowInMemoryFallback)
            {
                throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' is missing and in-memory fallback is disabled.");
            }

            services.AddDbContext<ApiGatewayDbContext>(builder => builder.UseInMemoryDatabase("ApiGateway"));
        }
        else
        {
            services.AddDbContext<ApiGatewayDbContext>(builder => builder.UseSqlServer(connectionString));
        }

        RegisterCoreServices(services);
        return services;
    }

    public static IServiceCollection AddCommonPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<ApiGatewayDbContext>(configureDbContext);
        RegisterCoreServices(services);
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddScoped<IApiAuditRepository, ApiAuditRepository>();
        services.AddScoped<IAuditRetentionService, AuditRetentionService>();
        services.AddHostedService<AuditRetentionHostedService>();
    }
}
