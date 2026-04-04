using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion.Connectors;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class CdcSocrataConnectorTests
{
    [Fact]
    public async Task FetchAsync_ParsesValidSocrataJsonResponse()
    {
        var json = """
        [
            {
                "reporting_jurisdiction": "Texas",
                "date": "2026-03-01T00:00:00.000",
                "pathogen_name": "SARS-CoV-2",
                "ptc_15d": "42"
            },
            {
                "reporting_jurisdiction": "California",
                "date": "2026-03-01T00:00:00.000",
                "pathogen_name": "Influenza",
                "ptc_15d": "18"
            }
        ]
        """;

        var handler = new FakeHttpHandler(json);
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(factory, Options.Create(new FeedIngestionOptions()), NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Test Wastewater",
            Type = FeedSourceType.CdcSocrata,
            Url = "test-dataset",
            SoqlQuery = "SELECT * LIMIT 10"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Records.Count);

        var first = result.Records[0];
        Assert.Equal("SARS-CoV-2", first.Disease);
        Assert.Equal("Texas", first.JurisdictionName);
        Assert.Equal(42, first.CaseCount);
        Assert.Equal(NormalizedRecordType.TrendDataPoint, first.RecordType);
        Assert.Contains("test-dataset", first.ExternalSourceId);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyForEmptyResponse()
    {
        var handler = new FakeHttpHandler("[]");
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(factory, Options.Create(new FeedIngestionOptions()), NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Empty Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = "empty-dataset"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnHttpError()
    {
        var handler = new FakeHttpHandler(statusCode: HttpStatusCode.InternalServerError);
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(factory, Options.Create(new FeedIngestionOptions()), NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Error Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = "error-dataset"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("500", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnMalformedJson()
    {
        var handler = new FakeHttpHandler("{not valid json}}}");
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(factory, Options.Create(new FeedIngestionOptions()), NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Bad JSON Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = "bad-dataset"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("JSON", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_HandlesDecimalCaseCountValues()
    {
        var json = """
        [
            {
                "reporting_jurisdiction": "Texas",
                "date": "2026-03-01",
                "pathogen_name": "Influenza",
                "ptc_15d": "15.7"
            }
        ]
        """;

        var handler = new FakeHttpHandler(json);
        var factory = new FakeHttpClientFactory(handler, "CdcSocrata", "https://data.cdc.gov/");
        var connector = new CdcSocrataConnector(factory, Options.Create(new FeedIngestionOptions()), NullLogger<CdcSocrataConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Decimal Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = "decimal-dataset"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(16, result.Records[0].CaseCount); // Rounded from 15.7
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string? _responseBody;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpHandler(string? responseBody = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_responseBody is not null)
                response.Content = new StringContent(_responseBody, Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class FakeHttpClientFactory(
        HttpMessageHandler handler,
        string clientName,
        string baseAddress) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (name != clientName)
                throw new ArgumentException($"Unexpected client name: {name}");

            return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
        }
    }
}
