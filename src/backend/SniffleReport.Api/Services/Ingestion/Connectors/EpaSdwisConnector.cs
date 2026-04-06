using System.Globalization;
using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class EpaSdwisConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<EpaSdwisConnector> logger) : IFeedConnector
{
    public FeedSourceType SourceType => FeedSourceType.EpaSdwis;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("EpaEnvirofacts");

        try
        {
            logger.LogInformation("Fetching EPA SDWIS data from {Url}", source.Url);
            var response = await client.GetAsync(source.Url.Trim(), ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(ct);
            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return FeedFetchResult.Failure("EPA SDWIS feed did not return a JSON array.");
            }

            var records = new List<NormalizedFeedRecord>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var record = ParseViolation(item);
                if (record is not null)
                {
                    records.Add(record);
                }
            }

            logger.LogInformation("Parsed {Count} EPA SDWIS records for {FeedName}", records.Count, source.Name);
            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching EPA SDWIS data for {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching EPA SDWIS data for {FeedName}", source.Name);
            return FeedFetchResult.Failure(ex.Message);
        }
    }

    private static NormalizedFeedRecord? ParseViolation(JsonElement item)
    {
        var pwsId = GetString(item, "pwsid");
        var violationId = GetString(item, "violation_id");
        var state = GetString(item, "primacy_agency_code")
            ?? GetString(item, "state");
        var county = GetString(item, "county_served")
            ?? GetString(item, "county_served_name");

        if (pwsId is null || violationId is null || state is null || county is null)
        {
            return null;
        }

        var jurisdictionName = $"{county}, {state}";
        var isOpen = !string.Equals(GetString(item, "violation_status"), "Resolved", StringComparison.OrdinalIgnoreCase);
        var category = GetString(item, "violation_category") ?? "Drinking water violation";
        var ruleName = GetString(item, "rule_family") ?? GetString(item, "rule_code") ?? "EPA SDWIS rule";
        var contaminantName = GetString(item, "contaminant_code") ?? GetString(item, "contaminant_name");

        return new NormalizedFeedRecord
        {
            ExternalSourceId = $"sdwis:{pwsId}:{violationId}",
            ParentExternalSourceId = $"sdwis-system:{pwsId}",
            RawPayloadJson = item.GetRawText(),
            RecordType = NormalizedRecordType.DrinkingWaterViolation,
            JurisdictionName = jurisdictionName,
            WaterSystemName = GetString(item, "pws_name") ?? pwsId,
            WaterSystemType = GetString(item, "pws_type_code"),
            WaterSystemAddress = GetString(item, "address_line1_txt"),
            WaterSystemCity = GetString(item, "city_name"),
            WaterSystemState = state,
            WaterSystemPostalCode = GetString(item, "zip_code"),
            CountyServed = county,
            PopulationServed = ParseNullableInt(GetString(item, "population_served_count")),
            ViolationCategory = category,
            RuleName = ruleName,
            ContaminantName = contaminantName,
            Summary = BuildSummary(category, ruleName, contaminantName, county),
            IsOpenViolation = isOpen,
            IdentifiedAt = ParseNullableDate(GetString(item, "violation_begin_date")),
            ResolvedAt = ParseNullableDate(GetString(item, "violation_end_date")),
            SourceDate = ParseNullableDate(GetString(item, "compliance_period_end_date"))
                ?? ParseNullableDate(GetString(item, "violation_begin_date")),
            SourceAttribution = "EPA Safe Drinking Water Information System"
        };
    }

    private static string BuildSummary(
        string category,
        string ruleName,
        string? contaminantName,
        string county)
    {
        if (!string.IsNullOrWhiteSpace(contaminantName))
        {
            return $"{category} under {ruleName} involving {contaminantName} for a public water system serving {county}.";
        }

        return $"{category} under {ruleName} for a public water system serving {county}.";
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var property))
        {
            return property.ValueKind switch
            {
                JsonValueKind.String => string.IsNullOrWhiteSpace(property.GetString()) ? null : property.GetString()?.Trim(),
                JsonValueKind.Number => property.GetRawText(),
                _ => null
            };
        }

        return null;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }
}
