using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class AlertService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<HealthAlert>> GetByRegionAsync(Guid regionId, AlertFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var query = BuildFilteredQuery(scopedRegionIds, filters);

        query = ApplySorting(query, filters.SortBy, filters.SortDirection);

        return await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByRegionAsync(Guid regionId, AlertFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        return await BuildFilteredQuery(scopedRegionIds, filters).CountAsync(cancellationToken);
    }

    public async Task<HealthAlert?> GetByIdAsync(Guid regionId, Guid alertId, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        return await dbContext.HealthAlerts
            .AsNoTracking()
            .Include(alert => alert.DiseaseTrends.OrderByDescending(trend => trend.Date))
            .SingleOrDefaultAsync(
                alert => alert.Id == alertId
                    && scopedRegionIds.Contains(alert.RegionId)
                    && alert.Status == AlertStatus.Published,
                cancellationToken);
    }

    public Task<IReadOnlyList<HealthAlert>> GetActiveByRegionAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        return GetByRegionAsync(
            regionId,
            new AlertFilters
            {
                Status = AlertStatus.Published,
                Page = 1,
                PageSize = 100
            },
            cancellationToken);
    }

    private IQueryable<HealthAlert> BuildFilteredQuery(IReadOnlyCollection<Guid> scopedRegionIds, AlertFilters filters)
    {
        IQueryable<HealthAlert> query = dbContext.HealthAlerts
            .AsNoTracking()
            .Where(alert => scopedRegionIds.Contains(alert.RegionId))
            .Where(alert => alert.Status == AlertStatus.Published)
            .Include(alert => alert.DiseaseTrends);

        if (filters.Severity is not null)
        {
            query = query.Where(alert => alert.Severity == filters.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Disease))
        {
            var normalizedDisease = filters.Disease.Trim().ToLowerInvariant();
            query = query.Where(alert => alert.Disease.ToLower().Contains(normalizedDisease));
        }

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(alert => alert.SourceDate >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(alert => alert.SourceDate <= filters.DateTo.Value);
        }

        return query;
    }

    private static IQueryable<HealthAlert> ApplySorting(IQueryable<HealthAlert> query, string? sortBy, string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "sourceDate" => descending
                ? query.OrderByDescending(alert => alert.SourceDate).ThenByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.SourceDate).ThenBy(alert => alert.CreatedAt),
            "caseCount" => descending
                ? query.OrderByDescending(alert => alert.CaseCount).ThenByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.CaseCount).ThenBy(alert => alert.CreatedAt),
            _ => descending
                ? query.OrderByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.CreatedAt)
        };
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
}
