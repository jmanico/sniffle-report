using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class HrsaConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<HrsaConnector> logger) : IFeedConnector
{
    public FeedSourceType SourceType => FeedSourceType.HrsaHealthCenter;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Hrsa");

        try
        {
            // HRSA Find a Health Center API — use US geographic center with large radius
            var url = source.Url.Trim();
            logger.LogInformation("Fetching HRSA health centers from {Url}", url);

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var records = new List<NormalizedFeedRecord>();

            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var center in results.EnumerateArray())
                {
                    var record = ParseCenter(center, source);
                    if (record is not null)
                        records.Add(record);
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var center in doc.RootElement.EnumerateArray())
                {
                    var record = ParseCenter(center, source);
                    if (record is not null)
                        records.Add(record);
                }
            }

            logger.LogInformation("Parsed {Count} HRSA health centers for {FeedName}", records.Count, source.Name);
            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching HRSA data for {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching HRSA data for {FeedName}", source.Name);
            return FeedFetchResult.Failure(ex.Message);
        }
    }

    private static NormalizedFeedRecord? ParseCenter(JsonElement center, FeedSource source)
    {
        var name = GetField(center, "name") ?? GetField(center, "siteName");
        var address = GetField(center, "address") ?? GetField(center, "street");
        var city = GetField(center, "city");
        var state = GetField(center, "state");
        var zip = GetField(center, "zip") ?? GetField(center, "postalCode");

        if (name is null || state is null) return null;

        var fullAddress = string.Join(", ",
            new[] { address, city, $"{state} {zip}" }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var lat = GetDouble(center, "latitude") ?? GetDouble(center, "lat");
        var lng = GetDouble(center, "longitude") ?? GetDouble(center, "lng") ?? GetDouble(center, "lon");
        var phone = GetField(center, "phone") ?? GetField(center, "telephone");
        var website = GetField(center, "website") ?? GetField(center, "url");

        var id = GetField(center, "id") ?? GetField(center, "facilityId") ?? name;
        var externalId = $"hrsa:{state}:{id}";

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = center.GetRawText(),
            RecordType = NormalizedRecordType.LocalResourceEntry,
            JurisdictionName = state,
            ResourceName = name.Length > 200 ? name[..200] : name,
            Address = fullAddress.Length > 300 ? fullAddress[..300] : fullAddress,
            Phone = phone?.Length > 40 ? phone[..40] : phone,
            Website = website?.Length > 500 ? website[..500] : website,
            Latitude = lat,
            Longitude = lng,
            ResourceType = ResourceType.Clinic,
            SourceAttribution = "HRSA Health Center Program"
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

    private static double? GetDouble(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var d))
                return d;
            if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var d2))
                return d2;
        }
        return null;
    }
}
