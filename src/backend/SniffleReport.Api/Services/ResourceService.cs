using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;

namespace SniffleReport.Api.Services;

public sealed class ResourceService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<ResourceListDto>> GetByRegionAsync(
        Guid regionId,
        ResourceFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var resources = await BuildFilteredQuery(scopedRegionIds, filters)
            .OrderBy(resource => resource.Name)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return resources.Select(resource => MapListDto(resource, null)).ToList();
    }

    public async Task<int> CountByRegionAsync(
        Guid regionId,
        ResourceFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        return await BuildFilteredQuery(scopedRegionIds, filters).CountAsync(cancellationToken);
    }

    public async Task<ResourceDetailDto?> GetByIdAsync(
        Guid regionId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        var resource = await dbContext.LocalResources
            .AsNoTracking()
            .Where(resource => scopedRegionIds.Contains(resource.RegionId))
            .Where(resource => resource.Id == resourceId)
            .SingleOrDefaultAsync(cancellationToken);

        return resource is null ? null : MapDetailDto(resource);
    }

    public async Task<IReadOnlyList<ResourceListDto>> SearchNearbyAsync(
        Guid regionId,
        double lat,
        double lng,
        double radiusMiles,
        ResourceFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        var candidates = await BuildFilteredQuery(scopedRegionIds, filters)
            .Where(resource => resource.Latitude.HasValue && resource.Longitude.HasValue)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(resource => new
            {
                Resource = resource,
                DistanceMiles = CalculateDistanceMiles(lat, lng, resource.Latitude!.Value, resource.Longitude!.Value)
            })
            .Where(result => result.DistanceMiles <= radiusMiles)
            .OrderBy(result => result.DistanceMiles)
            .ThenBy(result => result.Resource.Name)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(result => MapListDto(result.Resource, result.DistanceMiles))
            .ToList();
    }

    public async Task<int> CountNearbyAsync(
        Guid regionId,
        double lat,
        double lng,
        double radiusMiles,
        ResourceFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        var candidates = await BuildFilteredQuery(scopedRegionIds, filters)
            .Where(resource => resource.Latitude.HasValue && resource.Longitude.HasValue)
            .ToListAsync(cancellationToken);

        return candidates.Count(resource =>
            CalculateDistanceMiles(lat, lng, resource.Latitude!.Value, resource.Longitude!.Value) <= radiusMiles);
    }

    public static double CalculateDistanceMiles(double lat1, double lng1, double lat2, double lng2)
    {
        const double EarthRadiusMiles = 3958.8;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var startLat = DegreesToRadians(lat1);
        var endLat = DegreesToRadians(lat2);

        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(startLat) * Math.Cos(endLat) * Math.Pow(Math.Sin(dLng / 2), 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMiles * c;
    }

    private IQueryable<LocalResource> BuildFilteredQuery(IReadOnlyCollection<Guid> scopedRegionIds, ResourceFilters filters)
    {
        IQueryable<LocalResource> query = dbContext.LocalResources
            .AsNoTracking()
            .Where(resource => scopedRegionIds.Contains(resource.RegionId));

        if (filters.Type.HasValue)
        {
            query = query.Where(resource => resource.Type == filters.Type.Value);
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

    private static ResourceListDto MapListDto(LocalResource resource, double? distanceMiles)
    {
        return new ResourceListDto
        {
            Id = resource.Id,
            RegionId = resource.RegionId,
            Name = resource.Name,
            Type = resource.Type,
            Address = resource.Address,
            Phone = resource.Phone,
            Website = resource.Website,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            DistanceMiles = distanceMiles is null ? null : Math.Round(distanceMiles.Value, 2)
        };
    }

    private static ResourceDetailDto MapDetailDto(LocalResource resource)
    {
        return new ResourceDetailDto
        {
            Id = resource.Id,
            RegionId = resource.RegionId,
            Name = resource.Name,
            Type = resource.Type,
            Address = resource.Address,
            Phone = resource.Phone,
            Website = resource.Website,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            Hours = ParseHours(resource.HoursJson),
            Services = ParseServices(resource.ServicesJson)
        };
    }

    private static ResourceHoursDto ParseHours(string hoursJson)
    {
        var hours = JsonSerializer.Deserialize<Dictionary<string, string>>(hoursJson) ?? [];

        hours.TryGetValue("mon", out var mon);
        hours.TryGetValue("tue", out var tue);
        hours.TryGetValue("wed", out var wed);
        hours.TryGetValue("thu", out var thu);
        hours.TryGetValue("fri", out var fri);
        hours.TryGetValue("sat", out var sat);
        hours.TryGetValue("sun", out var sun);

        return new ResourceHoursDto
        {
            Mon = mon,
            Tue = tue,
            Wed = wed,
            Thu = thu,
            Fri = fri,
            Sat = sat,
            Sun = sun
        };
    }

    private static IReadOnlyList<string> ParseServices(string servicesJson)
    {
        return JsonSerializer.Deserialize<List<string>>(servicesJson) ?? [];
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
