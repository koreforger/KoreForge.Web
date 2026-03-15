using KF.RestApi.Common.Persistence;
using KF.RestApi.Common.Persistence.DependencyInjection;
using KF.RestApi.Common.Persistence.Entities;
using KF.RestApi.Common.Persistence.Repositories;
using KF.RestApi.Common.Persistence.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KF.RestApi.Common.Persistence.Tests;

public sealed class ApiAuditRepositoryTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");
    private ServiceProvider? _provider;

    [Fact]
    public async Task SaveAsync_PersistsAuditRecord()
    {
        using var scope = _provider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IApiAuditRepository>();

        var audit = new ApiCallAudit
        {
            ApiName = "Cases",
            Operation = "Get",
            Direction = "Outbound",
            StatusCode = 200,
            RequestTimestampUtc = DateTimeOffset.UtcNow,
            ResponseTimestampUtc = DateTimeOffset.UtcNow,
            DurationMs = 10,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        await repository.SaveAsync(audit);

        var results = await repository.GetRecentAsync("Cases", 5);
        Assert.Single(results);
        Assert.Equal("Get", results[0].Operation);
    }

    [Fact]
    public async Task GetRecentAsync_InvalidTake_Throws()
    {
        using var scope = _provider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IApiAuditRepository>();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetRecentAsync("Cases", 0));
    }

    [Fact]
    public async Task AddCommonPersistence_UsesInMemoryFallbackWhenConnectionMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:ConnectionStringName"] = "Missing",
            ["Persistence:AllowInMemoryFallback"] = "true"
        }).Build();

        var services = new ServiceCollection();
        services.AddCommonPersistence(configuration);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();
        await context.Database.EnsureCreatedAsync();

        var repository = scope.ServiceProvider.GetRequiredService<IApiAuditRepository>();
        await repository.SaveAsync(new ApiCallAudit
        {
            ApiName = "Fallback",
            Operation = "Ping",
            Direction = "Outbound",
            StatusCode = 200,
            RequestTimestampUtc = DateTimeOffset.UtcNow,
            ResponseTimestampUtc = DateTimeOffset.UtcNow,
            DurationMs = 1
        });

        var result = await repository.GetRecentAsync("Fallback", 1);
        Assert.Single(result);
    }

    public async Task InitializeAsync()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddOptions<AuditStoreOptions>();
        services.AddCommonPersistence(builder => builder.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
