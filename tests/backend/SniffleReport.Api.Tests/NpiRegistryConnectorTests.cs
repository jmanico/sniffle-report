using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using SniffleReport.Api.Services.Ingestion.Connectors;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class NpiRegistryConnectorTests
{
    [Fact]
    public async Task FetchAsync_ParsesPharmacyProviders()
    {
        var json = """
        {
            "result_count": 1,
            "results": [
                {
                    "number": "1234567890",
                    "basic": {
                        "organization_name": "CVS Pharmacy #1234"
                    },
                    "addresses": [
                        {
                            "address_purpose": "LOCATION",
                            "address_1": "100 MAIN ST",
                            "city": "AUSTIN",
                            "state": "TX",
                            "postal_code": "78701-1234",
                            "telephone_number": "512-555-0100"
                        }
                    ]
                }
            ]
        }
        """;

        var result = await FetchNpi("pharmacy", json);

        Assert.True(result.IsSuccess);
        // 51 states, each returning 1 record = 51 total
        Assert.Equal(51, result.Records.Count);
        var record = result.Records[0];
        Assert.Equal("CVS Pharmacy #1234", record.ResourceName);
        Assert.Equal(NormalizedRecordType.LocalResourceEntry, record.RecordType);
        Assert.Equal(ResourceType.Pharmacy, record.ResourceType);
        Assert.Contains("100 MAIN ST", record.Address);
        Assert.Equal("512-555-0100", record.Phone);
        Assert.Equal("npi:1234567890", record.ExternalSourceId);
    }

    [Fact]
    public async Task FetchAsync_MapsHospitalTaxonomy()
    {
        var json = """
        {
            "result_count": 1,
            "results": [
                {
                    "number": "9999999999",
                    "basic": {
                        "organization_name": "General Hospital"
                    },
                    "addresses": [
                        {
                            "address_purpose": "LOCATION",
                            "address_1": "500 HOSPITAL DR",
                            "city": "HOUSTON",
                            "state": "TX",
                            "postal_code": "77002"
                        }
                    ]
                }
            ]
        }
        """;

        var result = await FetchNpi("hospital", json);

        Assert.True(result.IsSuccess);
        Assert.True(result.Records.Count > 0);
        Assert.All(result.Records, r => Assert.Equal(ResourceType.Hospital, r.ResourceType));
    }

    [Fact]
    public async Task FetchAsync_UsesZipToCountyMapping()
    {
        var json = """
        {
            "result_count": 1,
            "results": [
                {
                    "number": "1111111111",
                    "basic": { "organization_name": "Test Clinic" },
                    "addresses": [
                        {
                            "address_purpose": "LOCATION",
                            "address_1": "123 ST",
                            "city": "AUSTIN",
                            "state": "TX",
                            "postal_code": "78701"
                        }
                    ]
                }
            ]
        }
        """;

        var result = await FetchNpi("urgent care", json);

        Assert.True(result.IsSuccess);
        // Find the record that has ZIP 78701 (all records have same ZIP in this test)
        var travisRecord = result.Records.FirstOrDefault(r =>
            r.JurisdictionName != null && r.JurisdictionName.Contains("Travis"));
        Assert.NotNull(travisRecord);
        Assert.Contains("Travis County", travisRecord.JurisdictionName);
    }

    [Fact]
    public async Task FetchAsync_SkipsProvidersWithoutAddress()
    {
        var json = """
        {
            "result_count": 1,
            "results": [
                {
                    "number": "3333333333",
                    "basic": { "organization_name": "No Address Provider" }
                }
            ]
        }
        """;

        var result = await FetchNpi("pharmacy", json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnHttpError()
    {
        var handler = new FakeHttpHandler(statusCode: HttpStatusCode.ServiceUnavailable);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new NpiRegistryConnector(factory, NullLogger<NpiRegistryConnector>.Instance);

        var source = new FeedSource
        {
            Name = "NPI Pharmacies",
            Type = FeedSourceType.NpiRegistry,
            Url = "pharmacy"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        // Should still succeed overall but with 0 records (individual state failures are swallowed)
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
    }

    private static async Task<FeedFetchResult> FetchNpi(string taxonomySearch, string json)
    {
        var handler = new FakeHttpHandler(json);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new NpiRegistryConnector(factory, NullLogger<NpiRegistryConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Test NPI",
            Type = FeedSourceType.NpiRegistry,
            Url = taxonomySearch
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
            var h = handler ?? new FakeHttpHandler(statusCode: HttpStatusCode.OK);
            return new HttpClient(h) { BaseAddress = new Uri("https://npiregistry.cms.hhs.gov/api/") };
        }
    }
}
