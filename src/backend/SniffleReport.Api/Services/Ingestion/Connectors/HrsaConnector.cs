using System.Globalization;
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

            var payload = await response.Content.ReadAsStringAsync(ct);
            var records = new List<NormalizedFeedRecord>();

            if (LooksLikeCsv(payload))
            {
                foreach (var row in CsvRecordReader.Parse(payload))
                {
                    var record = ParseCenter(row, source);
                    if (record is not null)
                    {
                        records.Add(record);
                    }
                }
            }
            else
            {
                using var doc = JsonDocument.Parse(payload);

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

    private static bool LooksLikeCsv(string payload)
    {
        var trimmed = payload.TrimStart();
        return !trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.StartsWith("[", StringComparison.Ordinal);
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

    private static NormalizedFeedRecord? ParseCenter(
        IReadOnlyDictionary<string, string> row,
        FeedSource source)
    {
        var name = CsvRecordReader.GetValue(row, "health_center_site_name", "site_name", "name");
        var state = CsvRecordReader.GetValue(row, "site_state_abbreviation", "state");

        if (name is null || state is null)
        {
            return null;
        }

        var address = CsvRecordReader.GetValue(row, "site_address", "street_address", "address");
        var city = CsvRecordReader.GetValue(row, "site_city", "city");
        var zip = CsvRecordReader.GetValue(row, "site_postal_code", "zip", "postal_code");
        var phone = CsvRecordReader.GetValue(row, "main_phone_number", "phone", "telephone");
        var website = CsvRecordReader.GetValue(row, "website_url", "website", "url");
        var id = CsvRecordReader.GetValue(row, "site_id", "id") ?? name;
        var latitude = ParseNullableDouble(CsvRecordReader.GetValue(row, "latitude", "site_latitude"));
        var longitude = ParseNullableDouble(CsvRecordReader.GetValue(row, "longitude", "site_longitude"));

        var fullAddress = string.Join(", ",
            new[] { address, city, $"{state} {zip}" }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new NormalizedFeedRecord
        {
            ExternalSourceId = $"hrsa:{state}:{id}",
            RawPayloadJson = JsonSerializer.Serialize(row),
            RecordType = NormalizedRecordType.LocalResourceEntry,
            JurisdictionName = state,
            ResourceName = name.Length > 200 ? name[..200] : name,
            Address = fullAddress.Length > 300 ? fullAddress[..300] : fullAddress,
            Phone = phone?.Length > 40 ? phone[..40] : phone,
            Website = website?.Length > 500 ? website[..500] : website,
            Latitude = latitude,
            Longitude = longitude,
            ResourceType = ResourceType.Clinic,
            SourceAttribution = "HRSA Health Center Service Delivery Sites"
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

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
