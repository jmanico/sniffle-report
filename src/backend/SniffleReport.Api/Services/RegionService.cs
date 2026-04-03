using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class RegionService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<Region>> GetAllAsync(RegionType? filter, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Regions.AsNoTracking();

        if (filter is not null)
        {
            query = query.Where(region => region.Type == filter.Value);
        }

        return await query
            .OrderBy(region => region.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Region?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Regions
            .AsNoTracking()
            .Include(region => region.Parent)
            .Include(region => region.Children)
            .SingleOrDefaultAsync(region => region.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Region>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();

        return await dbContext.Regions
            .AsNoTracking()
            .Where(region => region.Name.ToLower().Contains(normalizedQuery))
            .OrderBy(region => region.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Region>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Regions
            .AsNoTracking()
            .Where(region => region.ParentId == parentId)
            .OrderBy(region => region.Type)
            .ThenBy(region => region.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Region?> GetByZipAsync(string zip, CancellationToken cancellationToken = default)
    {
        var normalizedZip = zip.Trim();

        return dbContext.Regions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                region => region.Type == RegionType.Zip && region.Name == normalizedZip,
                cancellationToken);
    }
}
