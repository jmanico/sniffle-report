using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;

namespace SniffleReport.Api.Services;

public sealed class RegionHierarchyService(AppDbContext dbContext)
{
    /// <summary>
    /// Returns the root region ID plus all its descendant region IDs (BFS traversal).
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>> GetScopedRegionIdsAsync(Guid rootRegionId, CancellationToken cancellationToken = default)
    {
        var regions = await LoadRegionTreeAsync(cancellationToken);
        return CollectDescendants(regions, rootRegionId);
    }

    /// <summary>
    /// Builds a lookup mapping every region ID to its full set of descendant IDs (including self).
    /// Used by the snapshot builder to precompute all hierarchies in one pass.
    /// </summary>
    public async Task<Dictionary<Guid, HashSet<Guid>>> BuildFullHierarchyMapAsync(CancellationToken cancellationToken = default)
    {
        var regions = await LoadRegionTreeAsync(cancellationToken);

        // Build parent → children lookup
        var childrenByParent = new Dictionary<Guid, List<Guid>>();
        var allRegionIds = new HashSet<Guid>();

        foreach (var region in regions)
        {
            allRegionIds.Add(region.Id);
            if (region.ParentId is not null)
            {
                if (!childrenByParent.TryGetValue(region.ParentId.Value, out var children))
                {
                    children = [];
                    childrenByParent[region.ParentId.Value] = children;
                }
                children.Add(region.Id);
            }
        }

        var result = new Dictionary<Guid, HashSet<Guid>>(allRegionIds.Count);

        foreach (var regionId in allRegionIds)
        {
            result[regionId] = CollectDescendantsBfs(childrenByParent, regionId);
        }

        return result;
    }

    private async Task<List<RegionNode>> LoadRegionTreeAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Regions
            .AsNoTracking()
            .Select(region => new RegionNode { Id = region.Id, ParentId = region.ParentId })
            .ToListAsync(cancellationToken);
    }

    private static HashSet<Guid> CollectDescendants(List<RegionNode> regions, Guid rootRegionId)
    {
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

    private static HashSet<Guid> CollectDescendantsBfs(Dictionary<Guid, List<Guid>> childrenByParent, Guid rootRegionId)
    {
        var result = new HashSet<Guid> { rootRegionId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootRegionId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
                continue;

            foreach (var child in children)
            {
                if (result.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }

    private sealed class RegionNode
    {
        public Guid Id { get; init; }
        public Guid? ParentId { get; init; }
    }
}
