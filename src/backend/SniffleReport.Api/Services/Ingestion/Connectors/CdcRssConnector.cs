using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

public sealed class CdcRssConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<CdcRssConnector> logger) : IFeedConnector
{
    public FeedSourceType SourceType => FeedSourceType.CdcRss;

    public async Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("CdcRss");

        try
        {
            logger.LogInformation("Fetching CDC RSS feed from {Url}", source.Url);

            var response = await client.GetAsync(source.Url, ct);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var feed = SyndicationFeed.Load(reader);

            if (feed is null)
            {
                logger.LogWarning("RSS feed returned null for {FeedName}", source.Name);
                return FeedFetchResult.Success([]);
            }

            var records = new List<NormalizedFeedRecord>();

            foreach (var item in feed.Items)
            {
                var record = ParseItem(item, source);
                if (record is not null)
                    records.Add(record);
            }

            logger.LogInformation(
                "Parsed {Count} items from RSS feed {FeedName}",
                records.Count, source.Name);

            return FeedFetchResult.Success(records);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching CDC RSS feed {FeedName}", source.Name);
            return FeedFetchResult.Failure($"HTTP {ex.StatusCode}: {ex.Message}");
        }
        catch (XmlException ex)
        {
            logger.LogError(ex, "XML parse error for CDC RSS feed {FeedName}", source.Name);
            return FeedFetchResult.Failure($"XML parse error: {ex.Message}");
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout for CDC RSS feed {FeedName}", source.Name);
            return FeedFetchResult.Failure("Request timed out");
        }
    }

    private static NormalizedFeedRecord? ParseItem(SyndicationItem item, FeedSource source)
    {
        var title = item.Title?.Text;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var link = item.Links.FirstOrDefault()?.Uri?.AbsoluteUri;
        var summary = item.Summary?.Text;
        var publishDate = item.PublishDate.UtcDateTime;
        if (publishDate == DateTime.MinValue)
            publishDate = item.LastUpdatedTime.UtcDateTime;
        if (publishDate == DateTime.MinValue)
            publishDate = DateTime.UtcNow;

        // Use the item's GUID if available, otherwise hash the link
        var externalId = !string.IsNullOrWhiteSpace(item.Id)
            ? $"rss:{item.Id}"
            : $"rss:{ComputeHash(link ?? title)}";

        // Build raw payload for hashing
        var rawPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            id = item.Id,
            title,
            link,
            summary,
            publishDate = publishDate.ToString("O")
        });

        return new NormalizedFeedRecord
        {
            ExternalSourceId = externalId,
            RawPayloadJson = rawPayload,
            RecordType = NormalizedRecordType.NewsArticle,
            Title = title,
            Summary = StripHtmlTags(summary),
            SourceUrl = link,
            SourceDate = publishDate,
            SourceAttribution = source.Name,
            // RSS feeds are typically national-level; map to a default or skip region mapping
            JurisdictionName = null
        };
    }

    private static string? StripHtmlTags(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        // Simple HTML tag removal — good enough for RSS summaries
        var result = System.Text.RegularExpressions.Regex.Replace(
            html, "<[^>]+>", string.Empty,
            System.Text.RegularExpressions.RegexOptions.NonBacktracking);

        return System.Net.WebUtility.HtmlDecode(result).Trim();
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
