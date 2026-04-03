using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;

namespace SniffleReport.Api.Services;

public sealed class PreventionService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<PreventionGuide>> GetByRegionAsync(Guid regionId, PreventionFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var query = BuildFilteredQuery(scopedRegionIds, filters);

        return await query
            .OrderByDescending(guide => guide.CreatedAt)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByRegionAsync(Guid regionId, PreventionFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        return await BuildFilteredQuery(scopedRegionIds, filters).CountAsync(cancellationToken);
    }

    public async Task<PreventionGuide?> GetByIdAsync(Guid regionId, Guid guideId, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        return await dbContext.PreventionGuides
            .AsNoTracking()
            .Include(guide => guide.CostTiers)
            .SingleOrDefaultAsync(
                guide => guide.Id == guideId && scopedRegionIds.Contains(guide.RegionId),
                cancellationToken);
    }

    private IQueryable<PreventionGuide> BuildFilteredQuery(IReadOnlyCollection<Guid> scopedRegionIds, PreventionFilters filters)
    {
        IQueryable<PreventionGuide> query = dbContext.PreventionGuides
            .AsNoTracking()
            .Where(guide => scopedRegionIds.Contains(guide.RegionId))
            .Include(guide => guide.CostTiers);

        if (!string.IsNullOrWhiteSpace(filters.Disease))
        {
            var normalizedDisease = filters.Disease.Trim().ToLowerInvariant();
            query = query.Where(guide => guide.Disease.ToLower().Contains(normalizedDisease));
        }

        return query;
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
