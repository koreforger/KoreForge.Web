using KoreForge.RestApi.Common.Abstractions.DependencyInjection;
using KoreForge.RestApi.Common.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace KoreForge.RestApi.Common.Abstractions.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExternalApiOptions_BindsAndValidatesConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ExternalApis:Cases:BaseUrl"] = "https://api.example.com",
            ["ExternalApis:Cases:TimeoutSeconds"] = "45",
            ["ExternalApis:Cases:UserAgent"] = "TestAgent/1.0"
        });

        var services = new ServiceCollection();
        services.AddExternalApiOptions(configuration, "Cases");

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<ExternalApiOptions>>()
            .Get("Cases");

        Assert.Equal("https://api.example.com", options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);
        Assert.Equal("TestAgent/1.0", options.UserAgent);
    }

    [Fact]
    public void AddExternalApiOptions_InvalidConfiguration_Throws()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ExternalApis:Cases:TimeoutSeconds"] = "10"
        });

        var services = new ServiceCollection();
        services.AddExternalApiOptions(configuration, "Cases");

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<ExternalApiOptions>>();

        Assert.Throws<OptionsValidationException>(() => monitor.Get("Cases"));
    }

    [Fact]
    public void AddInternalApiOptions_BindsConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["InternalApis:Cases:BaseUrl"] = "https://internal.example.com",
            ["InternalApis:Cases:TimeoutSeconds"] = "15"
        });

        var services = new ServiceCollection();
        services.AddInternalApiOptions(configuration, "Cases");

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<InternalApiOptions>>()
            .Get("Cases");

        Assert.Equal("https://internal.example.com", options.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Timeout);
    }

    [Fact]
    public void AddPersistenceOptions_BindsDefaults()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Persistence:ConnectionStringName"] = "Audit",
            ["Persistence:AllowInMemoryFallback"] = "false"
        });

        var services = new ServiceCollection();
        services.AddPersistenceOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<PersistenceOptions>>()
            .Get(nameof(PersistenceOptions));

        Assert.Equal("Audit", options.ConnectionStringName);
        Assert.False(options.AllowInMemoryFallback);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
