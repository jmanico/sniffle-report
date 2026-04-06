using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Snapshots;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/status")]
public sealed class StatusController(AppDbContext dbContext) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("regions")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IReadOnlyList<RegionStatusDto>>> GetRegionStatus(
        CancellationToken cancellationToken)
    {
        var regions = await dbContext.Regions
            .AsNoTracking()
            .Include(r => r.Parent)
            .ToListAsync(cancellationToken);

        var snapshots = await dbContext.RegionSnapshots
            .AsNoTracking()
            .ToDictionaryAsync(s => s.RegionId, cancellationToken);

        var result = regions.Select(region =>
        {
            snapshots.TryGetValue(region.Id, out var snapshot);

            var resourceTotal = 0;
            if (snapshot is not null)
            {
                var counts = JsonSerializer.Deserialize<SnapshotResourceCounts>(
                    snapshot.ResourceCountsJson, JsonOptions);
                resourceTotal = counts?.Total ?? 0;
            }

            return new RegionStatusDto
            {
                RegionId = region.Id,
                Name = region.Name,
                Type = region.Type.ToString(),
                State = region.State,
                ParentName = region.Parent?.Name,
                ComputedAt = snapshot?.ComputedAt,
                PublishedAlertCount = snapshot?.PublishedAlertCount ?? 0,
                ResourceTotal = resourceTotal
            };
        })
        .OrderBy(r => r.Type)
        .ThenBy(r => r.State)
        .ThenBy(r => r.Name)
        .ToList();

        return Ok(result);
    }

    [HttpGet("feeds")]
    [ResponseCache(Duration = 30)]
    public async Task<ActionResult<IReadOnlyList<FeedStatusDto>>> GetFeedStatus(
        CancellationToken cancellationToken)
    {
        var feeds = await dbContext.FeedSources
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get latest sync log per feed
        var latestSyncs = await dbContext.FeedSyncLogs
            .AsNoTracking()
            .GroupBy(log => log.FeedSourceId)
            .Select(g => g.OrderByDescending(log => log.StartedAt).First())
            .ToDictionaryAsync(log => log.FeedSourceId, cancellationToken);
        var latestIngests = await dbContext.IngestedRecords
            .AsNoTracking()
            .GroupBy(record => record.FeedSourceId)
            .Select(g => new
            {
                FeedSourceId = g.Key,
                LastIngestedAt = g.Max(record => record.LastIngestedAt)
            })
            .ToDictionaryAsync(x => x.FeedSourceId, x => (DateTime?)x.LastIngestedAt, cancellationToken);

        var result = feeds.Select(feed =>
        {
            latestSyncs.TryGetValue(feed.Id, out var lastSync);
            latestIngests.TryGetValue(feed.Id, out var lastIngestedAt);

            return new FeedStatusDto
            {
                Id = feed.Id,
                Name = feed.Name,
                Type = feed.Type.ToString(),
                IsEnabled = feed.IsEnabled,
                LastSyncStatus = feed.LastSyncStatus.ToString(),
                LastSyncCompletedAt = feed.LastSyncCompletedAt ?? lastIngestedAt ?? lastSync?.CompletedAt ?? lastSync?.StartedAt,
                ConsecutiveFailureCount = feed.ConsecutiveFailureCount,
                LastRecordsCreated = lastSync?.RecordsCreated,
                LastRecordsFetched = lastSync?.RecordsFetched,
                LastRecordsSkippedUnmappable = lastSync?.RecordsSkippedUnmappable,
                LastSyncError = feed.LastSyncError
            };
        })
        .OrderBy(f => f.Name)
        .ToList();

        return Ok(result);
    }
}
