using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class PreventionService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<PreventionGuide>> GetAdminGuidesAsync(
        GetAdminPreventionGuidesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminFilteredQuery(query)
            .OrderByDescending(guide => guide.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminGuidesAsync(
        GetAdminPreventionGuidesQuery query,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminFilteredQuery(query).CountAsync(cancellationToken);
    }

    public Task<PreventionGuide?> GetAdminByIdAsync(Guid guideId, CancellationToken cancellationToken = default)
    {
        return dbContext.PreventionGuides
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(guide => guide.CostTiers)
            .SingleOrDefaultAsync(guide => guide.Id == guideId, cancellationToken);
    }

    public async Task<PreventionGuide> CreateAsync(
        CreatePreventionGuideRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var guide = new PreventionGuide
        {
            RegionId = request.RegionId,
            Disease = request.Disease.Trim(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            CostTiers = request.CostTiers.Select(MapCostTier).ToList()
        };

        dbContext.PreventionGuides.Add(guide);
        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Create,
            nameof(PreventionGuide),
            guide.Id,
            before: null,
            after: CreateGuideSnapshot(guide),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return guide;
    }

    public async Task<PreventionGuide?> UpdateAsync(
        Guid guideId,
        UpdatePreventionGuideRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var guide = await dbContext.PreventionGuides
            .IgnoreQueryFilters()
            .Include(existing => existing.CostTiers)
            .SingleOrDefaultAsync(existing => existing.Id == guideId, cancellationToken);

        if (guide is null)
        {
            return null;
        }

        var before = CreateGuideSnapshot(guide);

        guide.RegionId = request.RegionId;
        guide.Disease = request.Disease.Trim();
        guide.Title = request.Title.Trim();
        guide.Content = request.Content.Trim();

        dbContext.CostTiers.RemoveRange(guide.CostTiers);
        guide.CostTiers = request.CostTiers.Select(MapCostTier).ToList();

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Update,
            nameof(PreventionGuide),
            guide.Id,
            before,
            CreateGuideSnapshot(guide),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return guide;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid guideId,
        string justification,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var guide = await dbContext.PreventionGuides
            .IgnoreQueryFilters()
            .Include(existing => existing.CostTiers)
            .SingleOrDefaultAsync(existing => existing.Id == guideId, cancellationToken);

        if (guide is null)
        {
            return false;
        }

        var before = CreateGuideSnapshot(guide);
        guide.IsDeleted = true;
        guide.DeletedAt = DateTime.UtcNow;
        guide.DeletedBy = adminId == Guid.Empty ? null : adminId;

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Delete,
            nameof(PreventionGuide),
            guide.Id,
            before,
            CreateGuideSnapshot(guide),
            justification));
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

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

    private IQueryable<PreventionGuide> BuildAdminFilteredQuery(GetAdminPreventionGuidesQuery query)
    {
        IQueryable<PreventionGuide> guides = dbContext.PreventionGuides
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(guide => guide.CostTiers);

        if (query.RegionId.HasValue)
        {
            guides = guides.Where(guide => guide.RegionId == query.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Disease))
        {
            var normalizedDisease = query.Disease.Trim().ToLowerInvariant();
            guides = guides.Where(guide => guide.Disease.ToLower().Contains(normalizedDisease));
        }

        return guides;
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

    private static CostTier MapCostTier(AdminCostTierInput input)
    {
        return new CostTier
        {
            Type = input.Type,
            Price = input.Price,
            Provider = input.Provider.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
        };
    }

    private static object CreateGuideSnapshot(PreventionGuide guide)
    {
        return new
        {
            guide.Id,
            guide.RegionId,
            guide.Disease,
            guide.Title,
            guide.Content,
            guide.CreatedAt,
            guide.IsDeleted,
            guide.DeletedAt,
            guide.DeletedBy,
            CostTiers = guide.CostTiers
                .Select(tier => new
                {
                    tier.Type,
                    tier.Price,
                    tier.Provider,
                    tier.Notes
                })
                .ToList()
        };
    }
}
