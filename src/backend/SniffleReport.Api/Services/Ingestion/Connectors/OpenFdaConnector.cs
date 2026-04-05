using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class OpenFdaConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenFdaConnector> logger) : IFeedConnector
{
    public FeedSourceType SourceType => FeedSourceType.OpenFda;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OpenFda");

        try
        {
            var url = source.Url.Trim();
            logger.LogInformation("Fetching openFDA data from {Url}", url);

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var records = new List<NormalizedFeedRecord>();

            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    var record = ParseEnforcement(item, source);
                    if (record is not null)
                        records.Add(record);
                }
            }

            logger.LogInformation("Parsed {Count} openFDA records for {FeedName}", records.Count, source.Name);
            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching openFDA for {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching openFDA for {FeedName}", source.Name);
            return FeedFetchResult.Failure(ex.Message);
        }
    }

    private static NormalizedFeedRecord? ParseEnforcement(JsonElement item, FeedSource source)
    {
        var recallNumber = GetField(item, "recall_number");
        var product = GetField(item, "product_description");
        var reason = GetField(item, "reason_for_recall");
        var classification = GetField(item, "classification");
        var firm = GetField(item, "recalling_firm");
        var state = GetField(item, "state");
        var city = GetField(item, "city");
        var reportDate = GetField(item, "report_date");
        var status = GetField(item, "status");

        if (recallNumber is null || product is null) return null;

        var title = $"{classification ?? "Recall"}: {firm ?? "Unknown firm"} — {product}";
        if (title.Length > 300) title = title[..297] + "...";

        var summary = reason ?? $"Product recall: {product}";
        if (summary.Length > 2000) summary = summary[..1997] + "...";

        var externalId = $"fda:enforcement:{recallNumber}";

        DateTime? dataDate = null;
        if (reportDate is not null && DateTime.TryParseExact(reportDate, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
        {
            dataDate = dt.ToUniversalTime();
        }

        // Map to state if available
        var jurisdiction = state is not null ? state : null;

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = item.GetRawText(),
            RecordType = NormalizedRecordType.NewsArticle,
            Title = title,
            Summary = summary,
            JurisdictionName = jurisdiction,
            SourceDate = dataDate ?? DateTime.UtcNow,
            SourceUrl = $"https://api.fda.gov/drug/enforcement.json?search=recall_number:{recallNumber}",
            SourceAttribution = source.Name
        };
    }

    private static string? GetField(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
        }
        return null;
    }
}
