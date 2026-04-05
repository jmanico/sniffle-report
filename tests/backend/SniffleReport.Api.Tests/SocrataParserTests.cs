using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using SniffleReport.Api.Services.Ingestion.Connectors;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class SocrataParserTests
{
    [Fact]
    public async Task ParsePlacesRow_MapsCountyLevelHealthData()
    {
        var json = """
        [
            {
                "stateabbr": "TX",
                "locationname": "Travis",
                "category": "Health Outcomes",
                "measure": "Diabetes among adults",
                "data_value": "11.3",
                "data_value_type": "Age-adjusted prevalence"
            }
        ]
        """;

        var result = await FetchWithDataset("swc5-untb", json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Records);
        var record = result.Records[0];
        Assert.Equal("[Community Health] Diabetes among adults", record.Disease);
        Assert.Equal("Travis County, TX", record.JurisdictionName);
        Assert.Equal(11, record.CaseCount);
        Assert.Contains("places:", record.ExternalSourceId);
        Assert.Contains("11.3%", record.Title);
        Assert.Contains("age-adjusted", record.Title!.ToLower());
    }

    [Fact]
    public async Task ParsePlacesRow_SkipsNonAgeAdjustedRows()
    {
        var json = """
        [
            {
                "stateabbr": "TX",
                "locationname": "Travis",
                "measure": "Diabetes among adults",
                "data_value": "11.3",
                "data_value_type": "Crude prevalence"
            }
        ]
        """;

        var result = await FetchWithDataset("swc5-untb", json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task ParseOverdoseRow_MapsStateLevelData()
    {
        var json = """
        [
            {
                "state": "TX",
                "state_name": "Texas",
                "year": "2025",
                "month": "June",
                "indicator": "Opioids (T40.0-T40.4,T40.6)",
                "data_value": "4523"
            }
        ]
        """;

        var result = await FetchWithDataset("xkb8-kh2a", json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Records);
        var record = result.Records[0];
        Assert.Contains("Drug Overdose", record.Disease);
        Assert.Contains("Opioids", record.Disease);
        Assert.Equal("Texas", record.JurisdictionName);
        Assert.Equal(4523, record.CaseCount);
    }

    [Fact]
    public async Task ParseWastewaterRow_SplitsMultiCountyField()
    {
        var json = """
        [
            {
                "reporting_jurisdiction": "Minnesota",
                "date_end": "2025-08-01",
                "ptc_15d": "25",
                "percentile": "65",
                "county_names": "Anoka,Hennepin,Dakota",
                "key_plot_id": "site-123",
                "population_served": "50000"
            }
        ]
        """;

        var result = await FetchWithDataset("2ew6-ywp6", json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Records);
        var record = result.Records[0];
        // Should use first county (Anoka) not all three
        Assert.Equal("Anoka County, Minnesota", record.JurisdictionName);
        Assert.Equal(65, record.CaseCount); // percentile, not ptc_15d
    }

    [Fact]
    public async Task ParseNndssRow_SkipsAggregateRegions()
    {
        var json = """
        [
            { "states": "US RESIDENTS", "year": "2026", "week": "10", "label": "Flu", "m1": "100" },
            { "states": "NEW ENGLAND", "year": "2026", "week": "10", "label": "Flu", "m1": "50" },
            { "states": "CONNECTICUT", "year": "2026", "week": "10", "label": "Flu", "m1": "20" }
        ]
        """;

        var result = await FetchWithDataset("x9gk-5huc", json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Records); // Only Connecticut, aggregates skipped
        Assert.Equal("Connecticut", result.Records[0].JurisdictionName);
    }

    private static async Task<FeedFetchResult> FetchWithDataset(string datasetId, string json)
    {
        var handler = new FakeHttpHandler(json);
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(
            factory,
            Options.Create(new FeedIngestionOptions()),
            NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Test Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = datasetId,
            SoqlQuery = "SELECT * LIMIT 10"
        };

        return await connector.FetchAsync(source, CancellationToken.None);
    }

    private sealed class FakeHttpHandler(string? responseBody = null, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(statusCode);
            if (responseBody is not null)
                response.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler, string clientName, string baseAddress) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => name == clientName
            ? new HttpClient(handler) { BaseAddress = new Uri(baseAddress) }
            : throw new ArgumentException($"Unexpected client name: {name}");
    }
}
