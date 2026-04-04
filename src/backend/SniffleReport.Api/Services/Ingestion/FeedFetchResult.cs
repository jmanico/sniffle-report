namespace SniffleReport.Api.Services.Ingestion;

public sealed class FeedFetchResult
{
    public IReadOnlyList<NormalizedFeedRecord> Records { get; init; } = [];

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public static FeedFetchResult Success(IReadOnlyList<NormalizedFeedRecord> records) =>
        new() { Records = records, IsSuccess = true };

    public static FeedFetchResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
