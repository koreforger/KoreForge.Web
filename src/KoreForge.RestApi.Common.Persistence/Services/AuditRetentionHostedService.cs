using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KoreForge.RestApi.Common.Persistence.Services;

internal sealed class AuditRetentionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<Options.AuditStoreOptions> _options;
    private readonly ILogger<AuditRetentionHostedService> _logger;
    private readonly TimeSpan _interval;

    public AuditRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<Options.AuditStoreOptions> options,
        ILogger<AuditRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _interval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.CurrentValue.RetentionDays.HasValue)
        {
            _logger.LogInformation("Audit retention disabled (no RetentionDays configured).");
            return;
        }

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var retention = scope.ServiceProvider.GetRequiredService<IAuditRetentionService>();
            var removed = await retention.PurgeExpiredAsync(cancellationToken).ConfigureAwait(false);
            if (removed > 0)
            {
                _logger.LogInformation("Audit retention removed {Count} expired records.", removed);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit retention job failed.");
        }
    }
}