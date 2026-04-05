using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class CdcSocrataConnector(
    IHttpClientFactory httpClientFactory,
    IOptions<FeedIngestionOptions> options,
    ILogger<CdcSocrataConnector> logger) : IFeedConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Known dataset identifiers for schema-specific parsing
    private const string NndssDatasetId = "x9gk-5huc";
    private const string WastewaterDatasetId = "2ew6-ywp6";
    private const string CovidVaxDatasetId = "unsk-b7fc";
    private const string PlacesDatasetId = "swc5-untb";
    private const string OverdoseDatasetId = "xkb8-kh2a";

    public FeedSourceType SourceType => FeedSourceType.CdcSocrata;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("CdcSocrata");

        try
        {
            var appToken = options.Value.SocrataAppToken;
            if (!string.IsNullOrWhiteSpace(appToken))
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-App-Token", appToken);

            var requestUrl = BuildRequestUrl(source);
            logger.LogInformation("Fetching CDC Socrata data from {Url}", requestUrl);

            var response = await client.GetAsync(requestUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var rows = JsonSerializer.Deserialize<JsonElement[]>(json, JsonOptions);

            if (rows is null || rows.Length == 0)
            {
                logger.LogWarning("CDC Socrata returned empty result set for {FeedName}", source.Name);
                return FeedFetchResult.Success([]);
            }

            var records = new List<NormalizedFeedRecord>(rows.Length);

            foreach (var row in rows)
            {
                var record = ParseRow(row, source);
                if (record is not null)
                    records.Add(record);
            }

            logger.LogInformation(
                "Parsed {Parsed} of {Total} rows from {FeedName}",
                records.Count, rows.Length, source.Name);

            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching CDC Socrata feed {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parse error for CDC Socrata feed {FeedName}", source.Name);
            return FeedFetchResult.Failure($"JSON parse error: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout for CDC Socrata feed {FeedName}", source.Name);
            return FeedFetchResult.Failure("Request timed out");
        }
    }

    private static string BuildRequestUrl(FeedSource source)
    {
        var datasetId = source.Url.Trim();
        var baseUrl = $"resource/{datasetId}.json";

        if (!string.IsNullOrWhiteSpace(source.SoqlQuery))
            return $"{baseUrl}?$query={Uri.EscapeDataString(source.SoqlQuery)}";

        return $"{baseUrl}?$limit=10000&$order=:id";
    }

    private static NormalizedFeedRecord? ParseRow(JsonElement row, FeedSource source)
    {
        var datasetId = source.Url.Trim().ToLowerInvariant();

        return datasetId switch
        {
            NndssDatasetId => ParseNndssRow(row, source),
            WastewaterDatasetId => ParseWastewaterRow(row, source),
            CovidVaxDatasetId => ParseCovidVaxRow(row, source),
            PlacesDatasetId => ParsePlacesRow(row, source),
            OverdoseDatasetId => ParseOverdoseRow(row, source),
            _ => ParseGenericRow(row, source)
        };
    }

    /// <summary>
    /// NNDSS Weekly Data (x9gk-5huc)
    /// Fields: states, year, week, label (disease), m1 (current week), m2 (cumulative)
    /// states values are UPPERCASE: "CONNECTICUT", "NEW ENGLAND", "US RESIDENTS"
    /// </summary>
    private static NormalizedFeedRecord? ParseNndssRow(JsonElement row, FeedSource source)
    {
        var statesRaw = GetStringField(row, "states");
        var label = GetStringField(row, "label");
        var yearStr = GetStringField(row, "year");
        var weekStr = GetStringField(row, "week");
        var countStr = GetStringField(row, "m1");

        if (label is null || statesRaw is null)
            return null;

        // Skip aggregate rows like "US RESIDENTS", "NEW ENGLAND", "MID. ATLANTIC"
        if (statesRaw.Contains("RESIDENTS", StringComparison.OrdinalIgnoreCase)
            || statesRaw.Contains("ATLANTIC", StringComparison.OrdinalIgnoreCase)
            || statesRaw.Contains("ENGLAND", StringComparison.OrdinalIgnoreCase)
            || statesRaw.Contains("MOUNTAIN", StringComparison.OrdinalIgnoreCase)
            || statesRaw.Contains("PACIFIC", StringComparison.OrdinalIgnoreCase)
            || statesRaw.Contains("TOTAL", StringComparison.OrdinalIgnoreCase))
            return null;

        // Title-case the state name for region matching: "CONNECTICUT" → "Connecticut"
        var jurisdiction = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(statesRaw.ToLowerInvariant());

        var caseCount = TryParseCount(countStr);
        var dataDate = TryParseMmwrDate(yearStr, weekStr);

        var externalId = $"nndss:{yearStr ?? "0"}:{weekStr ?? "0"}:{jurisdiction}:{label}";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = label,
            JurisdictionName = jurisdiction,
            CaseCount = caseCount,
            DataDate = dataDate,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "CDC NNDSS Weekly Data"
        };
    }

    /// <summary>
    /// NWSS Wastewater Surveillance (2ew6-ywp6)
    /// Fields: reporting_jurisdiction, date_end, ptc_15d, county_names, percentile
    /// No pathogen field — this is COVID-19 wastewater data.
    /// ptc_15d = percent change over 15 days (can be negative)
    /// </summary>
    private static NormalizedFeedRecord? ParseWastewaterRow(JsonElement row, FeedSource source)
    {
        var jurisdiction = GetStringField(row, "reporting_jurisdiction");
        var dateStr = GetStringField(row, "date_end");
        var ptcStr = GetStringField(row, "ptc_15d");
        var percentileStr = GetStringField(row, "percentile");
        var county = GetStringField(row, "county_names");

        if (jurisdiction is null)
            return null;

        var dataDate = TryParseDate(dateStr);
        var percentChange = TryParseCount(ptcStr);

        // Use percentile (0-100 scale) as the primary metric for dashboard display.
        // Percent-change is informational but not meaningful as a "case count".
        var percentile = TryParseCount(percentileStr);
        var metricValue = (percentile is not null && percentile >= 0) ? percentile : null;

        // Fall back to percent change if percentile is unavailable
        if (metricValue is null && percentChange is not null && percentChange != -99)
        {
            metricValue = Math.Max(0, percentChange.Value);
        }

        var countyLabel = county is not null ? $":{county}" : "";
        var siteId = GetStringField(row, "key_plot_id") ?? GetStringField(row, "wwtp_id") ?? "0";
        var externalId = $"wastewater:{source.Url}:{jurisdiction}{countyLabel}:{dataDate?.ToString("yyyy-MM-dd") ?? "nodate"}:{siteId}";

        // Map to county if county_names is available, otherwise fall back to state.
        // CDC county_names may be comma-separated (multi-county sites) — use the first.
        // CDC uses bare names like "Honolulu", not "Honolulu County".
        var mappedJurisdiction = jurisdiction;
        if (county is not null)
        {
            var primaryCounty = county.Contains(',')
                ? county.Split(',')[0].Trim()
                : county.Trim();

            var hasKnownSuffix = primaryCounty.EndsWith("County", StringComparison.OrdinalIgnoreCase)
                || primaryCounty.EndsWith("Parish", StringComparison.OrdinalIgnoreCase)
                || primaryCounty.EndsWith("Borough", StringComparison.OrdinalIgnoreCase)
                || primaryCounty.EndsWith("Municipality", StringComparison.OrdinalIgnoreCase)
                || primaryCounty.EndsWith("City", StringComparison.OrdinalIgnoreCase);

            var countySuffix = hasKnownSuffix ? "" : " County";
            mappedJurisdiction = $"{primaryCounty}{countySuffix}, {jurisdiction}";
        }

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = "COVID-19 (Wastewater)",
            JurisdictionName = mappedJurisdiction,
            CaseCount = metricValue,
            DataDate = dataDate,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "CDC NWSS Wastewater Surveillance",
            Summary = BuildWastewaterSummary(percentile, percentChange)
        };
    }

    /// <summary>
    /// Generic fallback parser for unknown datasets.
    /// Tries common CDC field name patterns.
    /// </summary>
    private static NormalizedFeedRecord? ParseGenericRow(JsonElement row, FeedSource source)
    {
        var jurisdiction = GetStringField(row,
            "reporting_jurisdiction", "jurisdiction", "states", "state", "geo_value");
        var disease = GetStringField(row,
            "pathogen", "disease", "condition", "pathogen_name", "label");
        var dateStr = GetStringField(row,
            "date", "date_end", "collection_date", "date_updated", "week_end");
        var countStr = GetStringField(row,
            "m1", "metric_value", "count", "current_week", "value", "ptc_15d");

        if (jurisdiction is null && disease is null)
            return null;

        // Title-case if all uppercase
        if (jurisdiction is not null && jurisdiction == jurisdiction.ToUpperInvariant())
            jurisdiction = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(jurisdiction.ToLowerInvariant());

        var dataDate = TryParseDate(dateStr);
        var caseCount = TryParseCount(countStr);

        var externalId = $"socrata:{source.Url}:{jurisdiction ?? "unknown"}:{dataDate?.ToString("yyyy-MM-dd") ?? "nodate"}:{disease ?? "unknown"}";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = disease,
            JurisdictionName = jurisdiction,
            CaseCount = caseCount,
            DataDate = dataDate,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = $"CDC via data.cdc.gov/{source.Url}"
        };
    }

    /// <summary>
    /// COVID-19 Vaccination Distribution (unsk-b7fc)
    /// Fields: date, location (state code), administered, distributed
    /// </summary>
    private static NormalizedFeedRecord? ParseCovidVaxRow(JsonElement row, FeedSource source)
    {
        var location = GetStringField(row, "location");
        var dateStr = GetStringField(row, "date");
        var administered = GetStringField(row, "administered");
        var adminPer100k = GetStringField(row, "admin_per_100k");

        if (location is null || location.Length > 2)
            return null; // Skip non-state rows

        var dataDate = TryParseDate(dateStr);
        var count = TryParseCount(administered);

        var externalId = $"covidvax:{location}:{dataDate?.ToString("yyyy-MM-dd") ?? "nodate"}";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = "COVID-19 Vaccination",
            JurisdictionName = location,
            CaseCount = TryParseCount(adminPer100k),
            DataDate = dataDate,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "CDC COVID-19 Vaccination Distribution",
            Summary = count is not null ? $"Total doses administered: {count:N0}" : null
        };
    }

    /// <summary>
    /// CDC PLACES (swc5-untb) — County-level chronic disease prevalence
    /// Fields: stateabbr, locationname, measure, data_value, data_value_type, category
    /// </summary>
    private static NormalizedFeedRecord? ParsePlacesRow(JsonElement row, FeedSource source)
    {
        var stateAbbr = GetStringField(row, "stateabbr");
        var countyName = GetStringField(row, "locationname");
        var measure = GetStringField(row, "measure");
        var valueStr = GetStringField(row, "data_value");
        var dataValueType = GetStringField(row, "data_value_type");

        if (stateAbbr is null || countyName is null || measure is null)
            return null;

        // Only use age-adjusted prevalence to avoid duplicates
        if (dataValueType is not null && !dataValueType.Contains("Age-adjusted", StringComparison.OrdinalIgnoreCase))
            return null;

        var value = TryParseCount(valueStr);
        var jurisdiction = $"{countyName} County, {stateAbbr}";
        var externalId = $"places:{stateAbbr}:{countyName}:{measure}";

        var title = valueStr is not null
            ? $"{measure}: {valueStr}% (age-adjusted)"
            : measure;

        var category = GetStringField(row, "category") ?? "Health Outcomes";
        var summary = $"{category} — {measure}. Age-adjusted prevalence: {valueStr ?? "N/A"}%. "
            + $"Source: CDC PLACES (Behavioral Risk Factor Surveillance System). "
            + $"County: {countyName}, {stateAbbr}.";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = $"[Community Health] {measure}",
            JurisdictionName = jurisdiction,
            CaseCount = value,
            DataDate = DateTime.UtcNow,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "CDC PLACES",
            Title = title,
            Summary = summary
        };
    }

    /// <summary>
    /// Drug Overdose Deaths (xkb8-kh2a)
    /// Fields: state, state_name, year, month, indicator, data_value, predicted_value
    /// </summary>
    private static NormalizedFeedRecord? ParseOverdoseRow(JsonElement row, FeedSource source)
    {
        var stateName = GetStringField(row, "state_name");
        var stateCode = GetStringField(row, "state");
        var indicator = GetStringField(row, "indicator");
        var yearStr = GetStringField(row, "year");
        var monthStr = GetStringField(row, "month");
        var valueStr = GetStringField(row, "data_value") ?? GetStringField(row, "predicted_value");

        if (stateCode is null || indicator is null)
            return null;

        var value = TryParseCount(valueStr);
        var dataDate = TryParseOverdoseDate(yearStr, monthStr);
        var jurisdiction = stateName ?? stateCode;
        var disease = $"Drug Overdose ({indicator})";
        var externalId = $"overdose:{stateCode}:{yearStr}:{monthStr}:{indicator}";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = row.GetRawText(),
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = disease,
            JurisdictionName = jurisdiction,
            CaseCount = value,
            DataDate = dataDate,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "CDC Provisional Drug Overdose Deaths"
        };
    }

    private static DateTime? TryParseOverdoseDate(string? yearStr, string? monthStr)
    {
        if (!int.TryParse(yearStr, out var year) || year < 2000)
            return null;

        var monthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["January"] = 1, ["February"] = 2, ["March"] = 3, ["April"] = 4,
            ["May"] = 5, ["June"] = 6, ["July"] = 7, ["August"] = 8,
            ["September"] = 9, ["October"] = 10, ["November"] = 11, ["December"] = 12
        };

        if (monthStr is not null && monthNames.TryGetValue(monthStr, out var month))
            return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static string? BuildWastewaterSummary(int? percentile, int? percentChange)
    {
        var parts = new List<string>();
        if (percentile is not null)
            parts.Add($"Wastewater level: {percentile}th percentile");
        if (percentChange is not null && percentChange != -99)
            parts.Add($"15-day change: {percentChange}%");
        return parts.Count > 0 ? string.Join(". ", parts) : null;
    }

    private static string? GetStringField(JsonElement row, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (row.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        return null;
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return date.ToUniversalTime();

        return null;
    }

    private static DateTime? TryParseMmwrDate(string? yearStr, string? weekStr)
    {
        if (!int.TryParse(yearStr, out var year) || !int.TryParse(weekStr, out var week))
            return null;

        if (year < 2000 || year > 2100 || week < 1 || week > 53)
            return null;

        // MMWR week 1 starts on the first Sunday of the year that includes Jan 4
        // Approximate: use ISO week conversion
        try
        {
            return DateTime.SpecifyKind(
                ISOWeek.ToDateTime(year, Math.Min(week, ISOWeek.GetWeeksInYear(year)), DayOfWeek.Saturday),
                DateTimeKind.Utc);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryParseCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Skip flag values like "-", "N", "U", "NN"
        if (value.Length <= 2 && !char.IsDigit(value[0]) && value[0] != '-')
            return null;

        if (int.TryParse(value, out var intVal))
            return intVal;

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var dblVal))
            return (int)Math.Round(dblVal);

        return null;
    }
}
