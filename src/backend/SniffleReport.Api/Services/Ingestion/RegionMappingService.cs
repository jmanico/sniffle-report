using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class RegionMappingService(AppDbContext dbContext, ILogger<RegionMappingService> logger)
{
    private Dictionary<string, Guid>? _stateNameIndex;
    private Dictionary<string, Guid>? _stateCodeIndex;
    private Dictionary<string, Guid>? _countyIndex;

    public async Task<Guid?> ResolveRegionIdAsync(string? jurisdictionName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(jurisdictionName))
            return null;

        await EnsureCacheLoadedAsync(ct);

        var normalized = jurisdictionName.Trim();

        // 1. Exact name match on State regions
        if (_stateNameIndex!.TryGetValue(normalized.ToUpperInvariant(), out var stateId))
            return stateId;

        // 2. State code match (e.g. "TX" → "Texas")
        if (_stateCodeIndex!.TryGetValue(normalized.ToUpperInvariant(), out var codeId))
            return codeId;

        // 3. County-level match: "Travis County, TX" or "Travis County"
        if (_countyIndex!.TryGetValue(normalized.ToUpperInvariant(), out var countyId))
            return countyId;

        logger.LogWarning("Could not map jurisdiction {JurisdictionName} to any region", jurisdictionName);
        return null;
    }

    public void InvalidateCache()
    {
        _stateNameIndex = null;
        _stateCodeIndex = null;
        _countyIndex = null;
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (_stateNameIndex is not null)
            return;

        var regions = await dbContext.Regions
            .AsNoTracking()
            .Select(r => new { r.Id, r.Name, r.State, r.Type })
            .ToListAsync(ct);

        _stateNameIndex = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        _stateCodeIndex = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        _countyIndex = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in regions)
        {
            var nameKey = r.Name.ToUpperInvariant();
            var stateKey = r.State.ToUpperInvariant();

            switch (r.Type)
            {
                case RegionType.State:
                    _stateNameIndex.TryAdd(nameKey, r.Id);
                    _stateCodeIndex.TryAdd(stateKey, r.Id);
                    break;

                case RegionType.County:
                    // Index as "County Name" and "County Name, State"
                    _countyIndex.TryAdd(nameKey, r.Id);
                    _countyIndex.TryAdd($"{nameKey}, {stateKey}", r.Id);
                    break;

                case RegionType.Metro:
                    _countyIndex.TryAdd(nameKey, r.Id);
                    break;
            }
        }

        logger.LogInformation(
            "Region cache loaded: {StateCount} states, {CountyCount} county/metro entries",
            _stateNameIndex.Count,
            _countyIndex.Count);
    }
}
