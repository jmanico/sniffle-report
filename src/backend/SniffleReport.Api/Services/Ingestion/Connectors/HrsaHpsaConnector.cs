using System.Globalization;
using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class HrsaHpsaConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<HrsaHpsaConnector> logger) : IFeedConnector
{
    public FeedSourceType SourceType => FeedSourceType.HrsaHpsa;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Hrsa");

        try
        {
            logger.LogInformation("Fetching HRSA HPSA data from {Url}", source.Url);
            var response = await client.GetAsync(source.Url.Trim(), ct);
            response.EnsureSuccessStatusCode();

            var csv = await response.Content.ReadAsStringAsync(ct);
            var records = new List<NormalizedFeedRecord>();

            foreach (var row in CsvRecordReader.Parse(csv))
            {
                var record = ParseHpsa(row);
                if (record is not null)
                {
                    records.Add(record);
                }
            }

            logger.LogInformation("Parsed {Count} HRSA HPSA records for {FeedName}", records.Count, source.Name);
            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching HRSA HPSA data for {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching HRSA HPSA data for {FeedName}", source.Name);
            return FeedFetchResult.Failure(ex.Message);
        }
    }

    private static NormalizedFeedRecord? ParseHpsa(IReadOnlyDictionary<string, string> row)
    {
        var state = CsvRecordReader.GetValue(row, "state_abbreviation", "state")
            ?? CsvRecordReader.GetValue(row, "common_state_abbreviation");
        var countyName = CsvRecordReader.GetValue(row, "county_name", "county", "common_county_name");
        var areaName = CsvRecordReader.GetValue(row, "hpsa_name", "name", "designation_name");
        var discipline = CsvRecordReader.GetValue(row, "hpsa_discipline_class", "discipline", "hpsa_discipline");
        var designationType = CsvRecordReader.GetValue(row, "hpsa_designation_type", "designation_type");
        var status = CsvRecordReader.GetValue(row, "hpsa_status", "status") ?? "Designated";
        var hpsaId = CsvRecordReader.GetValue(row, "hpsa_id", "id");

        if (state is null || areaName is null || discipline is null || hpsaId is null)
        {
            return null;
        }

        var jurisdictionName = !string.IsNullOrWhiteSpace(countyName)
            ? $"{countyName}, {state}"
            : state;

        return new NormalizedFeedRecord
        {
            ExternalSourceId = $"hpsa:{hpsaId}",
            RawPayloadJson = JsonSerializer.Serialize(row),
            RecordType = NormalizedRecordType.ShortageAreaDesignation,
            JurisdictionName = jurisdictionName,
            AreaName = areaName,
            Discipline = discipline,
            DesignationType = designationType ?? "Geographic area",
            DesignationStatus = status,
            PopulationGroup = CsvRecordReader.GetValue(row, "population_type", "population_group"),
            HpsaScore = ParseNullableInt(CsvRecordReader.GetValue(row, "hpsa_score", "score")),
            PopulationToProviderRatio = ParseNullableDecimal(
                CsvRecordReader.GetValue(row, "population_to_provider_ratio", "populationproviderratio")),
            SourceDate = ParseNullableDate(
                CsvRecordReader.GetValue(row, "designation_date", "designation_last_update_date", "last_update_date")),
            SourceAttribution = "HRSA Health Professional Shortage Areas"
        };
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
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
