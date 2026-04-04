using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class FeedSourceDetailDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public FeedSourceType Type { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? SoqlQuery { get; init; }

    public TimeSpan PollingInterval { get; init; }

    public bool IsEnabled { get; init; }

    public bool AutoPublish { get; init; }

    public DateTime? LastSyncStartedAt { get; init; }

    public DateTime? LastSyncCompletedAt { get; init; }

    public FeedSyncStatus LastSyncStatus { get; init; }

    public string? LastSyncError { get; init; }

    public int ConsecutiveFailureCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public IReadOnlyList<FeedSyncLogDto> RecentSyncLogs { get; init; } = [];
}
