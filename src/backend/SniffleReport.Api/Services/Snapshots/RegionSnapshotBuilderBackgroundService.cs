using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;

namespace SniffleReport.Api.Services.Snapshots;

public sealed class RegionSnapshotBuilderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<SnapshotOptions> options,
    ILogger<RegionSnapshotBuilderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Region snapshot builder background service started");

        // Brief startup delay to let the app finish initialization
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in snapshot builder loop");
            }

            var interval = TimeSpan.FromMinutes(options.Value.RebuildIntervalMinutes);
            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("Region snapshot builder background service stopping");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasAnySnapshots = await dbContext.RegionSnapshots.AnyAsync(ct);

        if (hasAnySnapshots)
        {
            // Staleness check: skip rebuild if no new feed sync since last snapshot
            var latestSyncCompleted = await dbContext.FeedSyncLogs
                .MaxAsync(log => (DateTime?)log.CompletedAt, ct);

            var latestSnapshotComputed = await dbContext.RegionSnapshots
                .MaxAsync(snap => (DateTime?)snap.ComputedAt, ct);

            // If there have been no syncs at all, or the latest sync predates
            // the latest snapshot, there is nothing new to compute.
            if (!latestSyncCompleted.HasValue
                || (latestSnapshotComputed.HasValue
                    && latestSyncCompleted.Value <= latestSnapshotComputed.Value))
            {
                logger.LogDebug("No new feed syncs since last snapshot build, skipping");
                return;
            }
        }
        else
        {
            logger.LogInformation("No snapshots exist yet, performing initial build");
        }

        logger.LogInformation("Starting snapshot rebuild cycle");
        var builder = scope.ServiceProvider.GetRequiredService<RegionSnapshotBuilder>();
        await builder.RebuildAllAsync(ct);
    }
}
