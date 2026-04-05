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
