using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public interface IFeedConnector
{
    FeedSourceType SourceType { get; }

    Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct);
}
