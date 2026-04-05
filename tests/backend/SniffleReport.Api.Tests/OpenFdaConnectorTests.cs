using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using SniffleReport.Api.Services.Ingestion.Connectors;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class OpenFdaConnectorTests
{
    [Fact]
    public async Task FetchAsync_ParsesDrugEnforcementRecords()
    {
        var json = """
        {
            "results": [
                {
                    "recall_number": "D-0001-2026",
                    "product_description": "Ibuprofen Tablets, 200mg, 100 count",
                    "reason_for_recall": "Failed dissolution specifications",
                    "classification": "Class II",
                    "recalling_firm": "Acme Pharmaceuticals",
                    "state": "NJ",
                    "city": "Newark",
                    "report_date": "20260401",
                    "status": "Ongoing"
                }
            ]
        }
        """;

        var result = await FetchFda(json);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Records);
        var record = result.Records[0];
        Assert.Equal(NormalizedRecordType.NewsArticle, record.RecordType);
        Assert.Contains("Class II", record.Title);
        Assert.Contains("Acme Pharmaceuticals", record.Title);
        Assert.Contains("Failed dissolution", record.Summary);
        Assert.Equal("NJ", record.JurisdictionName);
        Assert.Equal("fda:enforcement:D-0001-2026", record.ExternalSourceId);
    }

    [Fact]
    public async Task FetchAsync_HandlesEmptyResults()
    {
        var json = """{ "results": [] }""";

        var result = await FetchFda(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task FetchAsync_SkipsRecordsWithoutRecallNumber()
    {
        var json = """
        {
            "results": [
                {
                    "product_description": "Some product",
                    "reason_for_recall": "Some reason"
                }
            ]
        }
        """;

        var result = await FetchFda(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnHttpError()
    {
        var handler = new FakeHttpHandler(statusCode: HttpStatusCode.TooManyRequests);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new OpenFdaConnector(factory, NullLogger<OpenFdaConnector>.Instance);

        var source = new FeedSource
        {
            Name = "openFDA Test",
            Type = FeedSourceType.OpenFda,
            Url = "drug/enforcement.json?limit=10"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private static async Task<FeedFetchResult> FetchFda(string json)
    {
        var handler = new FakeHttpHandler(json);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new OpenFdaConnector(factory, NullLogger<OpenFdaConnector>.Instance);

        var source = new FeedSource
        {
            Name = "openFDA Drug Enforcement",
            Type = FeedSourceType.OpenFda,
            Url = "drug/enforcement.json?limit=100"
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

    private sealed class FakeHttpClientFactory(HttpMessageHandler? handler = null) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var h = handler ?? new FakeHttpHandler();
            return new HttpClient(h) { BaseAddress = new Uri("https://api.fda.gov/") };
        }
    }
}
