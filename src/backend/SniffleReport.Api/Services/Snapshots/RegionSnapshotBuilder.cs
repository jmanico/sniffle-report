using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Models.Snapshots;

namespace SniffleReport.Api.Services.Snapshots;

public sealed class RegionSnapshotBuilder(
    AppDbContext dbContext,
    RegionHierarchyService regionHierarchy,
    IOptions<SnapshotOptions> options,
    ILogger<RegionSnapshotBuilder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int BatchSize = 50;

    public async Task RebuildAllAsync(CancellationToken ct)
    {
        var config = options.Value;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Load full region hierarchy
        var hierarchyMap = await regionHierarchy.BuildFullHierarchyMapAsync(ct);
        logger.LogDebug("Loaded hierarchy for {RegionCount} regions", hierarchyMap.Count);

        // Step 2: Batch-load all source data
        var alertsByRegion = await LoadAlertsByRegionAsync(ct);
        var trendsByAlert = await LoadTrendsByAlertAsync(ct);
        var newsByRegion = await LoadNewsByRegionAsync(ct);
        var preventionByRegion = await LoadPreventionByRegionAsync(ct);
        var resourcesByRegion = await LoadResourcesByRegionAsync(ct);
        var shortageAreasByRegion = await LoadShortageAreasByRegionAsync(ct);
        var waterSignalsByRegion = await LoadWaterSignalsByRegionAsync(ct);

        // Step 3: Load existing snapshots for upsert
        var existingSnapshots = await dbContext.RegionSnapshots
            .ToDictionaryAsync(s => s.RegionId, ct);

        // Step 4: Build snapshots per region in batches
        var now = DateTime.UtcNow;
        var batchCount = 0;

        foreach (var (regionId, descendantIds) in hierarchyMap)
        {
            var snapshot = BuildSnapshotForRegion(
                regionId, descendantIds, now, config,
                alertsByRegion, trendsByAlert, newsByRegion,
                preventionByRegion, resourcesByRegion, shortageAreasByRegion, waterSignalsByRegion);

            if (existingSnapshots.TryGetValue(regionId, out var existing))
            {
                existing.ComputedAt = snapshot.ComputedAt;
                existing.PublishedAlertCount = snapshot.PublishedAlertCount;
                existing.TopAlertsJson = snapshot.TopAlertsJson;
                existing.TrendHighlightsJson = snapshot.TrendHighlightsJson;
                existing.ResourceCountsJson = snapshot.ResourceCountsJson;
                existing.AccessSignalsJson = snapshot.AccessSignalsJson;
                existing.EnvironmentalSignalsJson = snapshot.EnvironmentalSignalsJson;
                existing.PreventionHighlightsJson = snapshot.PreventionHighlightsJson;
                existing.NewsHighlightsJson = snapshot.NewsHighlightsJson;
            }
            else
            {
                dbContext.RegionSnapshots.Add(snapshot);
            }

            batchCount++;
            if (batchCount >= BatchSize)
            {
                await dbContext.SaveChangesAsync(ct);
                batchCount = 0;
            }
        }

        if (batchCount > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Rebuilt snapshots for {RegionCount} regions in {ElapsedMs}ms",
            hierarchyMap.Count, stopwatch.ElapsedMilliseconds);
    }

    private RegionSnapshot BuildSnapshotForRegion(
        Guid regionId,
        HashSet<Guid> descendantIds,
        DateTime now,
        SnapshotOptions config,
        Dictionary<Guid, List<AlertData>> alertsByRegion,
        Dictionary<Guid, List<TrendData>> trendsByAlert,
        Dictionary<Guid, List<NewsData>> newsByRegion,
        Dictionary<Guid, List<PreventionData>> preventionByRegion,
        Dictionary<Guid, List<ResourceData>> resourcesByRegion,
        Dictionary<Guid, List<ShortageAreaData>> shortageAreasByRegion,
        Dictionary<Guid, List<WaterSignalData>> waterSignalsByRegion)
    {
        // Collect alerts from all descendant regions
        var alerts = CollectFromDescendants(alertsByRegion, descendantIds);
        var news = CollectFromDescendants(newsByRegion, descendantIds);
        var prevention = CollectFromDescendants(preventionByRegion, descendantIds);
        var resources = CollectFromDescendants(resourcesByRegion, descendantIds);
        var shortageAreas = CollectFromDescendants(shortageAreasByRegion, descendantIds);
        var waterSignals = CollectFromDescendants(waterSignalsByRegion, descendantIds);

        // Top alerts: prefer non-community-health alerts, but fall back to community
        // health indicators when that is all the region has so county pages do not
        // show "published alerts" with an empty alert list.
        var preferredAlerts = alerts
            .Where(a => !a.Disease.StartsWith("[Community Health]", StringComparison.Ordinal))
            .ToList();
        var alertPool = preferredAlerts.Count > 0 ? preferredAlerts : alerts;

        var topAlerts = alertPool
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.SourceDate)
            .Take(config.TopAlertsCount)
            .Select(a =>
            {
                var orderedTrendPoints = trendsByAlert.TryGetValue(a.AlertId, out var alertTrends)
                    ? alertTrends.OrderByDescending(t => t.Date).ToList()
                    : [];
                var previousPoint = orderedTrendPoints.Count > 1 ? orderedTrendPoints[1] : null;
                double? wowChangePercent = null;

                if (previousPoint is not null)
                {
                    wowChangePercent = previousPoint.CaseCount == 0
                        ? (a.CaseCount == 0 ? 0d : 100d)
                        : Math.Round(((a.CaseCount - previousPoint.CaseCount) / (double)previousPoint.CaseCount) * 100d, 1);
                }

                return new SnapshotAlertSummary
                {
                    AlertId = a.AlertId,
                    Disease = a.Disease,
                    Title = a.Title,
                    Summary = a.Summary,
                    Severity = a.Severity.ToString(),
                    CaseCount = a.CaseCount,
                    SourceAttribution = a.SourceAttribution,
                    SourceDate = a.SourceDate,
                    PreviousCaseCount = previousPoint?.CaseCount,
                    WowChangePercent = wowChangePercent,
                    PreviousSourceDate = previousPoint?.Date
                };
            })
            .ToList();

        // Trend highlights: compute WoW for each alert's disease
        var trendHighlights = BuildTrendHighlights(alerts, trendsByAlert, config);

        // Resource counts
        var resourceCounts = new SnapshotResourceCounts
        {
            Clinic = resources.Count(r => r.Type == ResourceType.Clinic),
            Pharmacy = resources.Count(r => r.Type == ResourceType.Pharmacy),
            VaccinationSite = resources.Count(r => r.Type == ResourceType.VaccinationSite),
            Hospital = resources.Count(r => r.Type == ResourceType.Hospital),
            Total = resources.Count
        };

        var accessSignals = shortageAreas
            .GroupBy(s => s.ExternalSourceId)
            .Select(g => g.OrderByDescending(x => x.SourceUpdatedAt).First())
            .OrderByDescending(s => s.HpsaScore ?? 0)
            .ThenBy(s => s.Discipline)
            .Take(4)
            .Select(s => new SnapshotAccessSignalSummary
            {
                DesignationId = s.DesignationId,
                AreaName = s.AreaName,
                Discipline = s.Discipline,
                DesignationType = s.DesignationType,
                Status = s.Status,
                PopulationGroup = s.PopulationGroup,
                HpsaScore = s.HpsaScore,
                PopulationToProviderRatio = s.PopulationToProviderRatio,
                SourceUpdatedAt = s.SourceUpdatedAt
            })
            .ToList();

        var environmentalSignals = waterSignals
            .Where(s => s.IsOpen)
            .GroupBy(s => s.ExternalSourceId)
            .Select(g => g.OrderByDescending(x => x.SourceUpdatedAt ?? x.IdentifiedAt).First())
            .OrderByDescending(s => s.PopulationServed ?? 0)
            .ThenByDescending(s => s.IdentifiedAt)
            .Take(4)
            .Select(s => new SnapshotEnvironmentalSignalSummary
            {
                ViolationId = s.ViolationId,
                WaterSystemName = s.WaterSystemName,
                ViolationCategory = s.ViolationCategory,
                RuleName = s.RuleName,
                ContaminantName = s.ContaminantName,
                Summary = s.Summary,
                IsOpen = s.IsOpen,
                PopulationServed = s.PopulationServed,
                IdentifiedAt = s.IdentifiedAt,
                SourceUpdatedAt = s.SourceUpdatedAt
            })
            .ToList();

        // Prevention highlights: most recently created
        var preventionHighlights = prevention
            .OrderByDescending(p => p.CreatedAt)
            .Take(config.PreventionHighlightsCount)
            .Select(p => new SnapshotPreventionSummary
            {
                GuideId = p.GuideId,
                Disease = p.Disease,
                Title = p.Title,
                HasCostTiers = p.HasCostTiers
            })
            .ToList();

        // News highlights: most recent with fact-check status
        var newsHighlights = news
            .OrderByDescending(n => n.PublishedAt)
            .Take(config.NewsHighlightsCount)
            .Select(n => new SnapshotNewsSummary
            {
                NewsItemId = n.NewsItemId,
                Headline = n.Headline,
                PublishedAt = n.PublishedAt,
                FactCheckStatus = n.FactCheckStatus?.ToString()
            })
            .ToList();

        return new RegionSnapshot
        {
            RegionId = regionId,
            ComputedAt = now,
            PublishedAlertCount = alerts.Count,
            TopAlertsJson = JsonSerializer.Serialize(topAlerts, JsonOptions),
            TrendHighlightsJson = JsonSerializer.Serialize(trendHighlights, JsonOptions),
            ResourceCountsJson = JsonSerializer.Serialize(resourceCounts, JsonOptions),
            AccessSignalsJson = JsonSerializer.Serialize(accessSignals, JsonOptions),
            EnvironmentalSignalsJson = JsonSerializer.Serialize(environmentalSignals, JsonOptions),
            PreventionHighlightsJson = JsonSerializer.Serialize(preventionHighlights, JsonOptions),
            NewsHighlightsJson = JsonSerializer.Serialize(newsHighlights, JsonOptions)
        };
    }

    private static List<SnapshotTrendHighlight> BuildTrendHighlights(
        List<AlertData> alerts,
        Dictionary<Guid, List<TrendData>> trendsByAlert,
        SnapshotOptions config)
    {
        var highlights = new List<SnapshotTrendHighlight>();

        foreach (var alert in alerts)
        {
            if (!trendsByAlert.TryGetValue(alert.AlertId, out var trends) || trends.Count < 2)
                continue;

            var orderedTrends = trends.OrderByDescending(t => t.Date).ToList();
            var latestDate = orderedTrends[0].Date;
            var wowCutoff = latestDate.AddDays(-7 * config.TrendWowWeeks);

            var latestCaseCount = orderedTrends[0].CaseCount;
            var previousPoints = orderedTrends.Where(t => t.Date <= wowCutoff).ToList();

            if (previousPoints.Count == 0)
                continue;

            var previousCaseCount = previousPoints[0].CaseCount;
            var wowChangePercent = previousCaseCount > 0
                ? ((double)(latestCaseCount - previousCaseCount) / previousCaseCount) * 100
                : latestCaseCount > 0 ? 100.0 : 0.0;

            highlights.Add(new SnapshotTrendHighlight
            {
                AlertId = alert.AlertId,
                Disease = alert.Disease,
                LatestCaseCount = latestCaseCount,
                PreviousCaseCount = previousCaseCount,
                WowChangePercent = Math.Round(wowChangePercent, 1),
                LatestDate = latestDate
            });
        }

        return highlights
            .OrderByDescending(h => Math.Abs(h.WowChangePercent))
            .Take(config.TrendHighlightsCount)
            .ToList();
    }

    private static List<T> CollectFromDescendants<T>(
        Dictionary<Guid, List<T>> dataByRegion,
        HashSet<Guid> descendantIds)
    {
        var result = new List<T>();
        foreach (var regionId in descendantIds)
        {
            if (dataByRegion.TryGetValue(regionId, out var items))
            {
                result.AddRange(items);
            }
        }
        return result;
    }

    private async Task<Dictionary<Guid, List<AlertData>>> LoadAlertsByRegionAsync(CancellationToken ct)
    {
        var alerts = await dbContext.HealthAlerts
            .AsNoTracking()
            .Where(a => a.Status == AlertStatus.Published)
            .Select(a => new AlertData
            {
                AlertId = a.Id,
                RegionId = a.RegionId,
                Disease = a.Disease,
                Title = a.Title,
                Summary = a.Summary,
                Severity = a.Severity,
                CaseCount = a.CaseCount,
                SourceAttribution = a.SourceAttribution,
                SourceDate = a.SourceDate
            })
            .ToListAsync(ct);

        return alerts.GroupBy(a => a.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<TrendData>>> LoadTrendsByAlertAsync(CancellationToken ct)
    {
        var trends = await dbContext.DiseaseTrends
            .AsNoTracking()
            .Where(t => t.Alert.Status == AlertStatus.Published)
            .Select(t => new TrendData
            {
                AlertId = t.AlertId,
                Date = t.Date,
                CaseCount = t.CaseCount
            })
            .ToListAsync(ct);

        return trends.GroupBy(t => t.AlertId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<NewsData>>> LoadNewsByRegionAsync(CancellationToken ct)
    {
        var news = await dbContext.NewsItems
            .AsNoTracking()
            .Include(n => n.FactCheck)
            .Select(n => new NewsData
            {
                NewsItemId = n.Id,
                RegionId = n.RegionId,
                Headline = n.Headline,
                PublishedAt = n.PublishedAt,
                FactCheckStatus = n.FactCheck != null ? n.FactCheck.Status : null
            })
            .ToListAsync(ct);

        return news.GroupBy(n => n.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<PreventionData>>> LoadPreventionByRegionAsync(CancellationToken ct)
    {
        var guides = await dbContext.PreventionGuides
            .AsNoTracking()
            .Include(g => g.CostTiers)
            .Select(g => new PreventionData
            {
                GuideId = g.Id,
                RegionId = g.RegionId,
                Disease = g.Disease,
                Title = g.Title,
                CreatedAt = g.CreatedAt,
                HasCostTiers = g.CostTiers.Any()
            })
            .ToListAsync(ct);

        return guides.GroupBy(g => g.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<ResourceData>>> LoadResourcesByRegionAsync(CancellationToken ct)
    {
        var resources = await dbContext.LocalResources
            .AsNoTracking()
            .Select(r => new ResourceData
            {
                RegionId = r.RegionId,
                Type = r.Type
            })
            .ToListAsync(ct);

        return resources.GroupBy(r => r.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<ShortageAreaData>>> LoadShortageAreasByRegionAsync(CancellationToken ct)
    {
        var designations = await dbContext.ShortageAreaDesignations
            .AsNoTracking()
            .Select(d => new ShortageAreaData
            {
                DesignationId = d.Id,
                RegionId = d.RegionId,
                ExternalSourceId = d.ExternalSourceId,
                AreaName = d.AreaName,
                Discipline = d.Discipline,
                DesignationType = d.DesignationType,
                Status = d.Status,
                PopulationGroup = d.PopulationGroup,
                HpsaScore = d.HpsaScore,
                PopulationToProviderRatio = d.PopulationToProviderRatio,
                SourceUpdatedAt = d.SourceUpdatedAt
            })
            .ToListAsync(ct);

        return designations.GroupBy(d => d.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<WaterSignalData>>> LoadWaterSignalsByRegionAsync(CancellationToken ct)
    {
        var violations = await dbContext.WaterSystemViolations
            .AsNoTracking()
            .Include(v => v.WaterSystem)
            .Select(v => new WaterSignalData
            {
                ViolationId = v.Id,
                RegionId = v.RegionId,
                ExternalSourceId = v.ExternalSourceId,
                WaterSystemName = v.WaterSystem.Name,
                ViolationCategory = v.ViolationCategory,
                RuleName = v.RuleName,
                ContaminantName = v.ContaminantName,
                Summary = v.Summary,
                IsOpen = v.IsOpen,
                PopulationServed = v.WaterSystem.PopulationServed,
                IdentifiedAt = v.IdentifiedAt,
                SourceUpdatedAt = v.SourceUpdatedAt
            })
            .ToListAsync(ct);

        return violations.GroupBy(v => v.RegionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private sealed class AlertData
    {
        public Guid AlertId { get; init; }
        public Guid RegionId { get; init; }
        public string Disease { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public AlertSeverity Severity { get; init; }
        public int CaseCount { get; init; }
        public string SourceAttribution { get; init; } = string.Empty;
        public DateTime SourceDate { get; init; }
    }

    private sealed class TrendData
    {
        public Guid AlertId { get; init; }
        public DateTime Date { get; init; }
        public int CaseCount { get; init; }
    }

    private sealed class NewsData
    {
        public Guid NewsItemId { get; init; }
        public Guid RegionId { get; init; }
        public string Headline { get; init; } = string.Empty;
        public DateTime PublishedAt { get; init; }
        public FactCheckStatus? FactCheckStatus { get; init; }
    }

    private sealed class PreventionData
    {
        public Guid GuideId { get; init; }
        public Guid RegionId { get; init; }
        public string Disease { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public bool HasCostTiers { get; init; }
    }

    private sealed class ResourceData
    {
        public Guid RegionId { get; init; }
        public ResourceType Type { get; init; }
    }

    private sealed class ShortageAreaData
    {
        public Guid DesignationId { get; init; }
        public Guid RegionId { get; init; }
        public string ExternalSourceId { get; init; } = string.Empty;
        public string AreaName { get; init; } = string.Empty;
        public string Discipline { get; init; } = string.Empty;
        public string DesignationType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? PopulationGroup { get; init; }
        public int? HpsaScore { get; init; }
        public decimal? PopulationToProviderRatio { get; init; }
        public DateTime? SourceUpdatedAt { get; init; }
    }

    private sealed class WaterSignalData
    {
        public Guid ViolationId { get; init; }
        public Guid RegionId { get; init; }
        public string ExternalSourceId { get; init; } = string.Empty;
        public string WaterSystemName { get; init; } = string.Empty;
        public string ViolationCategory { get; init; } = string.Empty;
        public string RuleName { get; init; } = string.Empty;
        public string? ContaminantName { get; init; }
        public string Summary { get; init; } = string.Empty;
        public bool IsOpen { get; init; }
        public int? PopulationServed { get; init; }
        public DateTime? IdentifiedAt { get; init; }
        public DateTime? SourceUpdatedAt { get; init; }
    }
}
