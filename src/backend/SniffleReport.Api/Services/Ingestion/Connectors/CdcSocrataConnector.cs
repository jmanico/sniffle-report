using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class CdcSocrataConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<CdcSocrataConnector> logger) : IFeedConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FeedSourceType SourceType => FeedSourceType.CdcSocrata;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("CdcSocrata");

        try
        {
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
        // source.Url contains the dataset identifier (e.g., "g653-nhgq")
        var datasetId = source.Url.Trim();
        var baseUrl = $"resource/{datasetId}.json";

        if (!string.IsNullOrWhiteSpace(source.SoqlQuery))
            return $"{baseUrl}?$query={Uri.EscapeDataString(source.SoqlQuery)}";

        // Default: fetch last 30 days of data, limit 10000 rows
        return $"{baseUrl}?$limit=10000&$order=:id";
    }

    private static NormalizedFeedRecord? ParseRow(JsonElement row, FeedSource source)
    {
        // CDC Socrata wastewater/surveillance datasets typically have these fields:
        // - jurisdiction or reporting_jurisdiction or state
        // - date or collection_date or mmwr_week + mmwr_year
        // - pathogen or disease or condition
        // - metric_value or count or current_week

        var jurisdiction = GetStringField(row,
            "reporting_jurisdiction", "jurisdiction", "state", "geo_value");
        var disease = GetStringField(row,
            "pathogen", "disease", "condition", "pathogen_name");
        var dateStr = GetStringField(row,
            "date", "collection_date", "date_updated", "week_end");
        var countStr = GetStringField(row,
            "metric_value", "count", "current_week", "value",
            "percent_change", "ptc_15d");

        if (jurisdiction is null && disease is null)
            return null;

        var dataDate = TryParseDate(dateStr);
        var caseCount = TryParseCount(countStr);

        // Build a stable external source ID
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

        // CDC dates come in various formats
        if (DateTime.TryParse(value, out var date))
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);

        return null;
    }

    private static int? TryParseCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Handle integer counts
        if (int.TryParse(value, out var intVal))
            return intVal;

        // Handle decimal values from metrics (round to int)
        if (double.TryParse(value, out var dblVal))
            return (int)Math.Round(dblVal);

        return null;
    }
}
