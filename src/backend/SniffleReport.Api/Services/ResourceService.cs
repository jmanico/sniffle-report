using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class ResourceService(AppDbContext dbContext, RegionHierarchyService regionHierarchy)
{
    public async Task<IReadOnlyList<LocalResource>> GetAdminResourcesAsync(
        GetAdminResourcesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminFilteredQuery(query)
            .OrderBy(resource => resource.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminResourcesAsync(
        GetAdminResourcesQuery query,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminFilteredQuery(query).CountAsync(cancellationToken);
    }

    public Task<LocalResource?> GetAdminByIdAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        return dbContext.LocalResources
            .AsNoTracking()
            .SingleOrDefaultAsync(resource => resource.Id == resourceId, cancellationToken);
    }

    public async Task<LocalResource> CreateAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var resource = new LocalResource
        {
            RegionId = request.RegionId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Address = request.Address.Trim(),
            Phone = NormalizeOptional(request.Phone),
            Website = NormalizeOptional(request.Website),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            HoursJson = SerializeHours(request.Hours),
            ServicesJson = SerializeServices(request.Services)
        };

        dbContext.LocalResources.Add(resource);
        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Create,
            nameof(LocalResource),
            resource.Id,
            before: null,
            after: CreateResourceSnapshot(resource),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return resource;
    }

    public async Task<LocalResource?> UpdateAsync(
        Guid resourceId,
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var resource = await dbContext.LocalResources
            .SingleOrDefaultAsync(existing => existing.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            return null;
        }

        var before = CreateResourceSnapshot(resource);

        resource.RegionId = request.RegionId;
        resource.Name = request.Name.Trim();
        resource.Type = request.Type;
        resource.Address = request.Address.Trim();
        resource.Phone = NormalizeOptional(request.Phone);
        resource.Website = NormalizeOptional(request.Website);
        resource.Latitude = request.Latitude;
        resource.Longitude = request.Longitude;
        resource.HoursJson = SerializeHours(request.Hours);
        resource.ServicesJson = SerializeServices(request.Services);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Update,
            nameof(LocalResource),
            resource.Id,
            before,
            CreateResourceSnapshot(resource),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return resource;
    }

    public async Task<bool> DeleteAsync(
        Guid resourceId,
        string justification,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var resource = await dbContext.LocalResources
            .SingleOrDefaultAsync(existing => existing.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            return false;
        }

        var before = CreateResourceSnapshot(resource);
        dbContext.LocalResources.Remove(resource);
        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Delete,
            nameof(LocalResource),
            resource.Id,
            before,
            after: null,
            justification));
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

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

    private IQueryable<LocalResource> BuildAdminFilteredQuery(GetAdminResourcesQuery query)
    {
        IQueryable<LocalResource> resources = dbContext.LocalResources.AsNoTracking();

        if (query.RegionId.HasValue)
        {
            resources = resources.Where(resource => resource.RegionId == query.RegionId.Value);
        }

        if (query.Type.HasValue)
        {
            resources = resources.Where(resource => resource.Type == query.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var normalizedName = query.Name.Trim().ToLowerInvariant();
            resources = resources.Where(resource => resource.Name.ToLower().Contains(normalizedName));
        }

        return resources;
    }

    private Task<IReadOnlyCollection<Guid>> GetScopedRegionIdsAsync(Guid rootRegionId, CancellationToken cancellationToken)
    {
        return regionHierarchy.GetScopedRegionIdsAsync(rootRegionId, cancellationToken);
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

    internal static ResourceHoursDto ParseHours(string hoursJson)
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

    internal static IReadOnlyList<string> ParseServices(string servicesJson)
    {
        return JsonSerializer.Deserialize<List<string>>(servicesJson) ?? [];
    }

    private static string SerializeHours(ResourceHoursDto hours)
    {
        var dictionary = new Dictionary<string, string>();

        AddIfPresent(dictionary, "mon", hours.Mon);
        AddIfPresent(dictionary, "tue", hours.Tue);
        AddIfPresent(dictionary, "wed", hours.Wed);
        AddIfPresent(dictionary, "thu", hours.Thu);
        AddIfPresent(dictionary, "fri", hours.Fri);
        AddIfPresent(dictionary, "sat", hours.Sat);
        AddIfPresent(dictionary, "sun", hours.Sun);

        return JsonSerializer.Serialize(dictionary);
    }

    private static string SerializeServices(IReadOnlyList<string> services)
    {
        return JsonSerializer.Serialize(
            services
                .Select(service => service.Trim())
                .Where(service => service.Length > 0)
                .ToList());
    }

    private static void AddIfPresent(IDictionary<string, string> dictionary, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dictionary[key] = value.Trim();
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static object CreateResourceSnapshot(LocalResource resource)
    {
        return new
        {
            resource.Id,
            resource.RegionId,
            resource.Name,
            resource.Type,
            resource.Address,
            resource.Phone,
            resource.Website,
            resource.Latitude,
            resource.Longitude,
            Hours = ParseHours(resource.HoursJson),
            Services = ParseServices(resource.ServicesJson)
        };
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
