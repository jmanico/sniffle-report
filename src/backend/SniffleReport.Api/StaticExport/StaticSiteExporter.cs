using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Models.Snapshots;

namespace SniffleReport.Api.StaticExport;

public sealed class StaticSiteExporter(AppDbContext dbContext, ILogger<StaticSiteExporter> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<ExportResult> ExportAsync(string outputDir, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var filesWritten = 0;

        Directory.CreateDirectory(outputDir);

        // 1. Load all data
        var regions = await dbContext.Regions
            .AsNoTracking()
            .Include(r => r.Parent)
            .ToListAsync(ct);

        var snapshots = await dbContext.RegionSnapshots
            .AsNoTracking()
            .ToDictionaryAsync(s => s.RegionId, ct);

        var resources = await dbContext.LocalResources
            .AsNoTracking()
            .Select(r => new ResourceExportData
            {
                Id = r.Id,
                RegionId = r.RegionId,
                Name = r.Name,
                Type = r.Type.ToString(),
                Address = r.Address,
                Phone = r.Phone,
                Website = r.Website
            })
            .ToListAsync(ct);

        var resourcesByRegion = resources
            .GroupBy(r => r.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var childrenByParent = regions
            .Where(r => r.ParentId.HasValue)
            .GroupBy(r => r.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToList());

        logger.LogInformation("Loaded {RegionCount} regions and {SnapshotCount} snapshots", regions.Count, snapshots.Count);

        // 2. Export states.json — index of all states
        var states = regions
            .Where(r => r.Type == RegionType.State && r.State != "US")
            .OrderBy(r => r.Name)
            .Select(r =>
            {
                snapshots.TryGetValue(r.Id, out var snap);
                var countyCount = regions.Count(c => c.ParentId == r.Id);
                return new
                {
                    id = r.Id,
                    name = r.Name,
                    code = r.State,
                    latitude = r.Latitude,
                    longitude = r.Longitude,
                    countyCount,
                    publishedAlertCount = snap?.PublishedAlertCount ?? 0,
                    resourceTotal = DeserializeResourceTotal(snap),
                    computedAt = snap?.ComputedAt
                };
            })
            .ToList();

        await WriteJsonAsync(Path.Combine(outputDir, "states.json"), states, ct);
        filesWritten++;

        // 3. Export per-state county lists: states/{stateCode}.json
        var statesDir = Path.Combine(outputDir, "states");
        Directory.CreateDirectory(statesDir);

        var stateRegions = regions.Where(r => r.Type == RegionType.State).ToList();
        foreach (var state in stateRegions)
        {
            var counties = regions
                .Where(r => r.ParentId == state.Id)
                .OrderBy(r => r.Name)
                .Select(r =>
                {
                    snapshots.TryGetValue(r.Id, out var snap);
                    return new
                    {
                        id = r.Id,
                        name = r.Name,
                        type = r.Type.ToString(),
                        state = r.State,
                        latitude = r.Latitude,
                        longitude = r.Longitude,
                        publishedAlertCount = snap?.PublishedAlertCount ?? 0,
                        resourceTotal = DeserializeResourceTotal(snap),
                        computedAt = snap?.ComputedAt
                    };
                })
                .ToList();

            // Also include the state's own snapshot as the header
            snapshots.TryGetValue(state.Id, out var stateSnap);
            var stateData = new
            {
                id = state.Id,
                name = state.Name,
                code = state.State,
                publishedAlertCount = stateSnap?.PublishedAlertCount ?? 0,
                resourceTotal = DeserializeResourceTotal(stateSnap),
                computedAt = stateSnap?.ComputedAt,
                counties
            };

            await WriteJsonAsync(Path.Combine(statesDir, $"{state.State}.json"), stateData, ct);
            filesWritten++;
        }

        // 4. Export per-region dashboard snapshots: regions/{regionId}.json
        var regionsDir = Path.Combine(outputDir, "regions");
        Directory.CreateDirectory(regionsDir);

        foreach (var region in regions)
        {
            if (!snapshots.TryGetValue(region.Id, out var snapshot))
                continue;

            var descendantIds = GetDescendantIds(region.Id, childrenByParent);
            var nearbyResources = descendantIds
                .SelectMany(id => resourcesByRegion.TryGetValue(id, out var items) ? items : [])
                .OrderBy(r => GetResourceSortOrder(r.Type))
                .ThenBy(r => r.Name)
                .Take(6)
                .ToList();

            var dashboard = new
            {
                regionId = region.Id,
                regionName = region.Name,
                regionType = region.Type.ToString(),
                state = region.State,
                parentName = region.Parent?.Name,
                parentId = region.ParentId,
                parentState = region.Parent?.State,
                computedAt = snapshot.ComputedAt,
                publishedAlertCount = snapshot.PublishedAlertCount,
                topAlerts = JsonSerializer.Deserialize<List<SnapshotAlertSummary>>(snapshot.TopAlertsJson, JsonOptions) ?? [],
                trendHighlights = JsonSerializer.Deserialize<List<SnapshotTrendHighlight>>(snapshot.TrendHighlightsJson, JsonOptions) ?? [],
                resourceCounts = JsonSerializer.Deserialize<SnapshotResourceCounts>(snapshot.ResourceCountsJson, JsonOptions) ?? new(),
                accessSignals = JsonSerializer.Deserialize<List<SnapshotAccessSignalSummary>>(snapshot.AccessSignalsJson, JsonOptions) ?? [],
                environmentalSignals = JsonSerializer.Deserialize<List<SnapshotEnvironmentalSignalSummary>>(snapshot.EnvironmentalSignalsJson, JsonOptions) ?? [],
                nearbyResources,
                preventionHighlights = JsonSerializer.Deserialize<List<SnapshotPreventionSummary>>(snapshot.PreventionHighlightsJson, JsonOptions) ?? [],
                newsHighlights = JsonSerializer.Deserialize<List<SnapshotNewsSummary>>(snapshot.NewsHighlightsJson, JsonOptions) ?? []
            };

            await WriteJsonAsync(Path.Combine(regionsDir, $"{region.Id}.json"), dashboard, ct);
            filesWritten++;
        }

        // 5. Export feed status: status.json
        var feeds = await dbContext.FeedSources.AsNoTracking().ToListAsync(ct);
        var latestSyncs = await dbContext.FeedSyncLogs
            .AsNoTracking()
            .GroupBy(log => log.FeedSourceId)
            .Select(g => g.OrderByDescending(log => log.StartedAt).First())
            .ToDictionaryAsync(log => log.FeedSourceId, ct);
        var latestIngests = await dbContext.IngestedRecords
            .AsNoTracking()
            .GroupBy(record => record.FeedSourceId)
            .Select(g => new
            {
                FeedSourceId = g.Key,
                LastIngestedAt = g.Max(record => record.LastIngestedAt)
            })
            .ToDictionaryAsync(x => x.FeedSourceId, x => (DateTime?)x.LastIngestedAt, ct);

        var feedStatus = feeds
        .Where(f => f.IsEnabled)
        .Select(f =>
        {
            latestSyncs.TryGetValue(f.Id, out var lastSync);
            latestIngests.TryGetValue(f.Id, out var lastIngestedAt);
            return new
            {
                name = f.Name,
                type = f.Type.ToString(),
                isEnabled = f.IsEnabled,
                lastSyncStatus = f.LastSyncStatus.ToString(),
                lastSyncCompletedAt = f.LastSyncCompletedAt ?? lastIngestedAt ?? lastSync?.CompletedAt ?? lastSync?.StartedAt,
                lastRecordsCreated = lastSync?.RecordsCreated,
                lastRecordsFetched = lastSync?.RecordsFetched
            };
        }).OrderBy(f => f.name).ToList();

        var withAlerts = snapshots.Values.Count(s => s.PublishedAlertCount > 0);
        var status = new
        {
            exportedAt = DateTime.UtcNow,
            totalRegions = regions.Count,
            totalSnapshots = snapshots.Count,
            regionsWithAlerts = withAlerts,
            feeds = feedStatus
        };

        await WriteJsonAsync(Path.Combine(outputDir, "status.json"), status, ct);
        filesWritten++;

        // 6. Export national news: news.json
        var newsItems = await dbContext.NewsItems
            .AsNoTracking()
            .Include(n => n.FactCheck)
            .OrderByDescending(n => n.PublishedAt)
            .Take(100)
            .Select(n => new
            {
                id = n.Id,
                headline = n.Headline,
                sourceUrl = n.SourceUrl,
                publishedAt = n.PublishedAt,
                factCheckStatus = n.FactCheck != null ? n.FactCheck.Status.ToString() : (string?)null
            })
            .ToListAsync(ct);

        await WriteJsonAsync(Path.Combine(outputDir, "news.json"), newsItems, ct);
        filesWritten++;

        stopwatch.Stop();
        logger.LogInformation(
            "Static export complete: {Files} files written to {Dir} in {Ms}ms",
            filesWritten, outputDir, stopwatch.ElapsedMilliseconds);

        return new ExportResult { FilesWritten = filesWritten, OutputDirectory = outputDir };
    }

    private static async Task WriteJsonAsync(string path, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    private static int DeserializeResourceTotal(Models.Entities.RegionSnapshot? snapshot)
    {
        if (snapshot is null) return 0;
        var counts = JsonSerializer.Deserialize<SnapshotResourceCounts>(snapshot.ResourceCountsJson, JsonOptions);
        return counts?.Total ?? 0;
    }

    private static HashSet<Guid> GetDescendantIds(Guid regionId, Dictionary<Guid, List<Guid>> childrenByParent)
    {
        var result = new HashSet<Guid> { regionId };
        var stack = new Stack<Guid>();
        stack.Push(regionId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!childrenByParent.TryGetValue(current, out var children))
                continue;

            foreach (var child in children)
            {
                if (result.Add(child))
                {
                    stack.Push(child);
                }
            }
        }

        return result;
    }

    private static int GetResourceSortOrder(string resourceType) => resourceType switch
    {
        "Pharmacy" => 0,
        "Clinic" => 1,
        "Hospital" => 2,
        "VaccinationSite" => 3,
        _ => 4
    };
}

public sealed class ExportResult
{
    public int FilesWritten { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
}

file sealed class ResourceExportData
{
    public Guid Id { get; init; }
    public Guid RegionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Website { get; init; }
}
