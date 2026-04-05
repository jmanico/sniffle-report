using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class RegionMappingService(AppDbContext dbContext, ILogger<RegionMappingService> logger)
{
    private const string NationalRegionName = "United States";

    private Dictionary<string, Guid>? _stateNameIndex;
    private Dictionary<string, Guid>? _stateCodeIndex;
    private Dictionary<string, Guid>? _countyIndex;
    private Dictionary<string, Guid>? _normalizedCountyIndex;
    private Guid? _nationalRegionId;

    public async Task<Guid?> ResolveRegionIdAsync(string? jurisdictionName, CancellationToken ct)
    {
        await EnsureCacheLoadedAsync(ct);

        // Null jurisdiction → fall back to national region (for RSS feeds, etc.)
        if (string.IsNullOrWhiteSpace(jurisdictionName))
            return _nationalRegionId;

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

        // 4. Fuzzy fallback: try common naming variations
        var fuzzyMatch = TryFuzzyCountyMatch(normalized);
        if (fuzzyMatch.HasValue)
            return fuzzyMatch.Value;

        logger.LogWarning("Could not map jurisdiction {JurisdictionName} to any region", jurisdictionName);
        return null;
    }

    public void InvalidateCache()
    {
        _stateNameIndex = null;
        _stateCodeIndex = null;
        _countyIndex = null;
        _normalizedCountyIndex = null;
        _nationalRegionId = null;
    }

    private Guid? TryFuzzyCountyMatch(string input)
    {
        // Try without spaces: "Du Page County" -> "DuPageCounty"
        var noSpaces = input.Replace(" ", "");
        if (_normalizedCountyIndex!.TryGetValue(noSpaces, out var id))
            return id;

        // Alaska: "Anchorage County" -> "Anchorage Municipality"
        // Try replacing "County" with "Borough" or "Municipality"
        if (input.Contains("County", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var alt in new[] { "Borough", "Municipality", "Census Area", "City and Borough" })
            {
                var variant = input.Replace("County", alt, StringComparison.OrdinalIgnoreCase);
                if (_countyIndex!.TryGetValue(variant, out var altId))
                    return altId;
            }
        }

        // Virginia independent cities: "Charlottesville City County" -> try as "Charlottesville city"
        if (input.Contains("City County", StringComparison.OrdinalIgnoreCase))
        {
            var asCity = input.Replace("City County", "city", StringComparison.OrdinalIgnoreCase);
            if (_countyIndex!.TryGetValue(asCity, out var cityId))
                return cityId;
        }

        // Try stripping " County" suffix and matching just the name + state
        // e.g., "Carson City County, Nevada" -> "Carson City, Nevada"
        if (input.Contains(" County,", StringComparison.OrdinalIgnoreCase))
        {
            var withoutCounty = input.Replace(" County,", ",", StringComparison.OrdinalIgnoreCase);
            if (_countyIndex!.TryGetValue(withoutCounty, out var stripId))
                return stripId;
        }

        return null;
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
        _nationalRegionId = null;

        // First pass: build state indexes so we can resolve state names for counties
        var stateNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in regions.Where(r => r.Type == RegionType.State))
        {
            var nameKey = r.Name.ToUpperInvariant();
            var stateKey = r.State.ToUpperInvariant();

            if (r.Name.Equals(NationalRegionName, StringComparison.OrdinalIgnoreCase))
                _nationalRegionId = r.Id;

            _stateNameIndex.TryAdd(nameKey, r.Id);
            _stateCodeIndex.TryAdd(stateKey, r.Id);
            stateNameByCode.TryAdd(stateKey, nameKey);
        }

        // Second pass: counties and metros
        foreach (var r in regions.Where(r => r.Type is RegionType.County or RegionType.Metro))
        {
            var nameKey = r.Name.ToUpperInvariant();
            var stateKey = r.State.ToUpperInvariant();

            if (r.Type == RegionType.County)
            {
                // Index as "County Name", "County Name, StateCode", and "County Name, StateName"
                _countyIndex.TryAdd(nameKey, r.Id);
                _countyIndex.TryAdd($"{nameKey}, {stateKey}", r.Id);

                if (stateNameByCode.TryGetValue(stateKey, out var stateName))
                {
                    _countyIndex.TryAdd($"{nameKey}, {stateName}", r.Id);
                }
            }
            else
            {
                _countyIndex.TryAdd(nameKey, r.Id);
            }
        }

        // Build normalized alias index for fuzzy matching
        _normalizedCountyIndex = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, id) in _countyIndex)
        {
            // Index without spaces for "Du Page" -> "DuPage", "De Kalb" -> "DeKalb"
            var noSpaces = key.Replace(" ", "");
            _normalizedCountyIndex.TryAdd(noSpaces, id);
        }

        if (_nationalRegionId is null)
            logger.LogWarning("No '{NationalRegion}' region found — RSS items with no jurisdiction will be skipped", NationalRegionName);

        logger.LogInformation(
            "Region cache loaded: {StateCount} states, {CountyCount} county/metro entries, national={HasNational}",
            _stateNameIndex.Count,
            _countyIndex.Count,
            _nationalRegionId.HasValue);
    }
}
