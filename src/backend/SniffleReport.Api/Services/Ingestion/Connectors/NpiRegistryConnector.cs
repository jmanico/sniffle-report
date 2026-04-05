using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class NpiRegistryConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<NpiRegistryConnector> logger) : IFeedConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FeedSourceType SourceType => FeedSourceType.NpiRegistry;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NpiRegistry");

        try
        {
            // The Url field contains the taxonomy search text, e.g. "pharmacy"
            var taxonomySearch = source.Url.Trim();
            var resourceType = MapTaxonomyToResourceType(taxonomySearch);
            var allRecords = new List<NormalizedFeedRecord>();

            // Query state by state to stay within API limits (200 per request)
            var states = GetStateAbbreviations();

            foreach (var state in states)
            {
                if (ct.IsCancellationRequested) break;

                var stateRecords = await FetchStateAsync(client, taxonomySearch, state, resourceType, source, ct);
                allRecords.AddRange(stateRecords);
            }

            logger.LogInformation(
                "NPI Registry fetched {Count} {Type} providers across {States} states for {FeedName}",
                allRecords.Count, resourceType, states.Length, source.Name);

            // NPI API returns max 200 per state — log if any states hit the limit
            if (allRecords.Count >= states.Length * 200)
                logger.LogWarning("NPI results may be truncated — API limit of 200 per state reached for {FeedName}", source.Name);

            return FeedFetchResult.Success(allRecords);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching NPI Registry for {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout for NPI Registry {FeedName}", source.Name);
            return FeedFetchResult.Failure("Request timed out");
        }
    }

    private async Task<List<NormalizedFeedRecord>> FetchStateAsync(
        HttpClient client,
        string taxonomyCode,
        string stateCode,
        ResourceType resourceType,
        FeedSource source,
        CancellationToken ct)
    {
        var url = $"?version=2.1&taxonomy_description={Uri.EscapeDataString(taxonomyCode)}&state={stateCode}&limit=200";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("results", out var results))
            return [];

        var records = new List<NormalizedFeedRecord>();

        foreach (var provider in results.EnumerateArray())
        {
            var record = ParseProvider(provider, stateCode, resourceType, source);
            if (record is not null)
                records.Add(record);
        }

        return records;
    }

    private static NormalizedFeedRecord? ParseProvider(
        JsonElement provider,
        string stateCode,
        ResourceType resourceType,
        FeedSource source)
    {
        var npi = GetField(provider, "number");
        if (npi is null) return null;

        // Get the practice location address (prefer practice over mailing)
        JsonElement? address = null;
        if (provider.TryGetProperty("addresses", out var addresses))
        {
            foreach (var addr in addresses.EnumerateArray())
            {
                var purpose = addr.TryGetProperty("address_purpose", out var p) ? p.GetString() : null;
                if (purpose == "LOCATION")
                {
                    address = addr;
                    break;
                }
                address ??= addr;
            }
        }

        if (address is null) return null;

        var name = GetBasicField(provider, "organization_name")
            ?? $"{GetBasicField(provider, "first_name")} {GetBasicField(provider, "last_name")}".Trim();

        if (string.IsNullOrWhiteSpace(name)) return null;

        var addr1 = GetField(address.Value, "address_1") ?? "";
        var city = GetField(address.Value, "city") ?? "";
        var state = GetField(address.Value, "state") ?? stateCode;
        var zip = GetField(address.Value, "postal_code") ?? "";
        var phone = GetField(address.Value, "telephone_number");
        var fullAddress = $"{addr1}, {city}, {state} {zip}".Trim().Trim(',');

        // Map to county using "City, State" as jurisdiction
        var jurisdiction = $"{city}, {state}".Trim().Trim(',');

        return new NormalizedFeedRecord
        {
            ExternalSourceId = $"npi:{npi}",
            RawPayloadJson = provider.GetRawText(),
            RecordType = NormalizedRecordType.LocalResourceEntry,
            JurisdictionName = state,
            ResourceName = name.Length > 200 ? name[..200] : name,
            Address = fullAddress.Length > 300 ? fullAddress[..300] : fullAddress,
            Phone = phone?.Length > 40 ? phone[..40] : phone,
            ResourceType = resourceType,
            SourceAttribution = "CMS NPI Registry"
        };
    }

    private static string? GetBasicField(JsonElement provider, string fieldName)
    {
        if (provider.TryGetProperty("basic", out var basic) &&
            basic.TryGetProperty(fieldName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            var str = value.GetString();
            return string.IsNullOrWhiteSpace(str) ? null : str.Trim();
        }
        return null;
    }

    private static string? GetField(JsonElement element, string fieldName)
    {
        if (element.TryGetProperty(fieldName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var str = value.GetString();
            return string.IsNullOrWhiteSpace(str) ? null : str.Trim();
        }
        return null;
    }

    private static ResourceType MapTaxonomyToResourceType(string searchText)
    {
        if (searchText.Contains("pharmacy", StringComparison.OrdinalIgnoreCase))
            return ResourceType.Pharmacy;
        if (searchText.Contains("hospital", StringComparison.OrdinalIgnoreCase))
            return ResourceType.Hospital;
        return ResourceType.Clinic;
    }

    private static string[] GetStateAbbreviations() =>
    [
        "AL","AK","AZ","AR","CA","CO","CT","DC","DE","FL","GA","HI","ID","IL","IN",
        "IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH",
        "NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT",
        "VT","VA","WA","WV","WI","WY"
    ];
}
