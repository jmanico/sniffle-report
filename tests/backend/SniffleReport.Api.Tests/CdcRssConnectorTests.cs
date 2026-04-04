using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion.Connectors;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class CdcRssConnectorTests
{
    private const string ValidRss = """
        <?xml version="1.0" encoding="utf-8"?>
        <rss version="2.0">
          <channel>
            <title>CDC MMWR</title>
            <link>https://cdc.gov/mmwr</link>
            <description>CDC MMWR Reports</description>
            <item>
              <title>Weekly Disease Summary March 2026</title>
              <link>https://cdc.gov/mmwr/2026/march</link>
              <guid>mmwr-2026-march-001</guid>
              <description>Summary of reportable diseases.</description>
              <pubDate>Sun, 01 Mar 2026 12:00:00 +0000</pubDate>
            </item>
            <item>
              <title>Travel Health Notice Region X</title>
              <link>https://cdc.gov/travel/notice-123</link>
              <guid>travel-notice-123</guid>
              <description>Important travel health advisory.</description>
              <pubDate>Tue, 03 Mar 2026 08:00:00 +0000</pubDate>
            </item>
          </channel>
        </rss>
        """;

    [Fact]
    public async Task FetchAsync_ParsesValidRssFeed()
    {
        var handler = new FakeHttpHandler(ValidRss);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new CdcRssConnector(factory, NullLogger<CdcRssConnector>.Instance);

        var source = new FeedSource
        {
            Name = "CDC MMWR",
            Type = FeedSourceType.CdcRss,
            Url = "https://cdc.gov/mmwr/rss"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(2, result.Records.Count);

        var first = result.Records[0];
        Assert.Equal(NormalizedRecordType.NewsArticle, first.RecordType);
        Assert.Contains("Weekly Disease Summary", first.Title);
        Assert.Equal("rss:mmwr-2026-march-001", first.ExternalSourceId);
        Assert.Equal("https://cdc.gov/mmwr/2026/march", first.SourceUrl);
    }

    [Fact]
    public async Task FetchAsync_StripsHtmlFromSummary()
    {
        var handler = new FakeHttpHandler(ValidRss);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new CdcRssConnector(factory, NullLogger<CdcRssConnector>.Instance);

        var source = new FeedSource
        {
            Name = "CDC MMWR",
            Type = FeedSourceType.CdcRss,
            Url = "https://cdc.gov/mmwr/rss"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMessage}");
        var first = result.Records[0];
        Assert.DoesNotContain("<p>", first.Summary);
        Assert.Contains("Summary of reportable diseases", first.Summary);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnHttpError()
    {
        var handler = new FakeHttpHandler(statusCode: HttpStatusCode.ServiceUnavailable);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new CdcRssConnector(factory, NullLogger<CdcRssConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Error RSS",
            Type = FeedSourceType.CdcRss,
            Url = "https://cdc.gov/error-rss"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("503", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailureOnInvalidXml()
    {
        var handler = new FakeHttpHandler("this is not xml at all");
        var factory = new FakeHttpClientFactory(handler);
        var connector = new CdcRssConnector(factory, NullLogger<CdcRssConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Bad XML",
            Type = FeedSourceType.CdcRss,
            Url = "https://cdc.gov/bad-xml"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("XML", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAsync_HandlesEmptyFeed()
    {
        var emptyRss = """
            <?xml version="1.0" encoding="utf-8"?>
            <rss version="2.0">
              <channel>
                <title>Empty Feed</title>
              </channel>
            </rss>
            """;

        var handler = new FakeHttpHandler(emptyRss);
        var factory = new FakeHttpClientFactory(handler);
        var connector = new CdcRssConnector(factory, NullLogger<CdcRssConnector>.Instance);

        var source = new FeedSource
        {
            Name = "Empty RSS",
            Type = FeedSourceType.CdcRss,
            Url = "https://cdc.gov/empty-rss"
        };

        var result = await connector.FetchAsync(source, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Records);
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
                response.Content = new StringContent(_responseBody, Encoding.UTF8, "application/xml");
            return Task.FromResult(response);
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }
}
