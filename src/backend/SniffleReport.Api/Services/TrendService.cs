using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;

namespace SniffleReport.Api.Services;

public sealed class TrendService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<TrendSeriesDto>> GetAggregateByRegionAsync(
        Guid regionId,
        TrendFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var trendPoints = await BuildTrendPointQuery(scopedRegionIds, filters)
            .ToListAsync(cancellationToken);

        return MapSeries(trendPoints)
            .OrderByDescending(series => series.DataPoints.Max(point => point.Date))
            .ThenBy(series => series.AlertTitle)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToList();
    }

    public async Task<int> CountAggregateByRegionAsync(
        Guid regionId,
        TrendFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        return await BuildTrendPointQuery(scopedRegionIds, filters)
            .Select(trend => trend.AlertId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<TrendSeriesDto?> GetByAlertAsync(
        Guid regionId,
        Guid alertId,
        TrendFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var trendPoints = await BuildTrendPointQuery(scopedRegionIds, filters)
            .Where(trend => trend.AlertId == alertId)
            .ToListAsync(cancellationToken);

        return MapSeries(trendPoints).SingleOrDefault();
    }

    private IQueryable<TrendPoint> BuildTrendPointQuery(
        IReadOnlyCollection<Guid> scopedRegionIds,
        TrendFilters filters)
    {
        var query = dbContext.HealthAlerts
            .AsNoTracking()
            .Where(alert => scopedRegionIds.Contains(alert.RegionId))
            .Where(alert => alert.Status == Models.Enums.AlertStatus.Published)
            .SelectMany(
                alert => alert.DiseaseTrends,
                (alert, trend) => new TrendPoint
                {
                    AlertId = alert.Id,
                    RegionId = alert.RegionId,
                    Disease = alert.Disease,
                    AlertTitle = alert.Title,
                    SourceAttribution = alert.SourceAttribution,
                    Date = trend.Date,
                    CaseCount = trend.CaseCount,
                    Source = trend.Source,
                    SourceDate = trend.SourceDate
                });

        if (!string.IsNullOrWhiteSpace(filters.Disease))
        {
            var normalizedDisease = filters.Disease.Trim().ToLowerInvariant();
            query = query.Where(alert => alert.Disease.ToLower().Contains(normalizedDisease));
        }

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(trend => trend.Date >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(trend => trend.Date <= filters.DateTo.Value);
        }

        return query;
    }

    private static IReadOnlyList<TrendSeriesDto> MapSeries(IReadOnlyCollection<TrendPoint> trendPoints)
    {
        return trendPoints
            .GroupBy(
                point => new
                {
                    point.AlertId,
                    point.RegionId,
                    point.Disease,
                    point.AlertTitle,
                    point.SourceAttribution
                })
            .Select(group => new TrendSeriesDto
            {
                AlertId = group.Key.AlertId,
                RegionId = group.Key.RegionId,
                Disease = group.Key.Disease,
                AlertTitle = group.Key.AlertTitle,
                SourceAttribution = group.Key.SourceAttribution,
                DataPoints = group
                    .OrderBy(point => point.Date)
                    .Select(point => new TrendDataPointDto
                    {
                        Date = point.Date,
                        CaseCount = point.CaseCount,
                        Source = point.Source,
                        SourceDate = point.SourceDate
                    })
                    .ToList()
            })
            .ToList();
    }

    private async Task<IReadOnlyCollection<Guid>> GetScopedRegionIdsAsync(Guid rootRegionId, CancellationToken cancellationToken)
    {
        var regions = await dbContext.Regions
            .AsNoTracking()
            .Select(region => new { region.Id, region.ParentId })
            .ToListAsync(cancellationToken);

        var scopedRegionIds = new HashSet<Guid> { rootRegionId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootRegionId);

        while (queue.Count > 0)
        {
            var currentRegionId = queue.Dequeue();
            var childRegionIds = regions
                .Where(region => region.ParentId == currentRegionId)
                .Select(region => region.Id);

            foreach (var childRegionId in childRegionIds)
            {
                if (scopedRegionIds.Add(childRegionId))
                {
                    queue.Enqueue(childRegionId);
                }
            }
        }

        return scopedRegionIds;
    }

    private sealed class TrendPoint
    {
        public Guid AlertId { get; init; }

        public Guid RegionId { get; init; }

        public string Disease { get; init; } = string.Empty;

        public string AlertTitle { get; init; } = string.Empty;

        public string SourceAttribution { get; init; } = string.Empty;

        public DateTime Date { get; init; }

        public int CaseCount { get; init; }

        public string Source { get; init; } = string.Empty;

        public DateTime SourceDate { get; init; }
    }
}
