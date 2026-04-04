using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class FeedSourceListDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public FeedSourceType Type { get; init; }

    public string Url { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public FeedSyncStatus LastSyncStatus { get; init; }

    public DateTime? LastSyncCompletedAt { get; init; }

    public int ConsecutiveFailureCount { get; init; }
}
