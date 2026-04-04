using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class FeedPollingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FeedPollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollCheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxBackoffInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Feed polling background service started");

        // Brief startup delay to let the app finish initialization
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollDueSourcesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in feed polling loop");
            }

            await Task.Delay(PollCheckInterval, stoppingToken);
        }

        logger.LogInformation("Feed polling background service stopping");
    }

    private async Task PollDueSourcesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var enabledSources = await dbContext.FeedSources
            .Where(s => s.IsEnabled)
            .AsNoTracking()
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var source in enabledSources)
        {
            if (!IsDue(source, now))
                continue;

            // Each source gets its own scope for isolation
            await SyncSourceAsync(source.Id, ct);
        }
    }

    private async Task SyncSourceAsync(Guid sourceId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

            var source = await dbContext.FeedSources.FindAsync([sourceId], ct);
            if (source is null || !source.IsEnabled)
                return;

            logger.LogInformation("Starting scheduled sync for feed {FeedName}", source.Name);
            var syncLog = await ingestionService.ExecuteSyncAsync(source, ct);
            logger.LogInformation(
                "Feed {FeedName} sync completed: status={Status}, created={Created}, updated={Updated}, skipped={Skipped}",
                source.Name, syncLog.Status, syncLog.RecordsCreated,
                syncLog.RecordsUpdated, syncLog.RecordsSkippedDuplicate);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to sync feed source {SourceId}", sourceId);
        }
    }

    private static bool IsDue(Models.Entities.FeedSource source, DateTime now)
    {
        if (source.LastSyncStartedAt is null)
            return true;

        var interval = source.PollingInterval;

        // Apply exponential backoff on consecutive failures
        if (source.ConsecutiveFailureCount > 0)
        {
            var backoffMultiplier = Math.Pow(2, Math.Min(source.ConsecutiveFailureCount, 6));
            var backoffInterval = TimeSpan.FromTicks((long)(interval.Ticks * backoffMultiplier));
            interval = backoffInterval > MaxBackoffInterval ? MaxBackoffInterval : backoffInterval;
        }

        return now - source.LastSyncStartedAt.Value >= interval;
    }
}
