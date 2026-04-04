using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class FeedSource : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public FeedSourceType Type { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? SoqlQuery { get; set; }

    public TimeSpan PollingInterval { get; set; }

    public bool IsEnabled { get; set; }

    public bool AutoPublish { get; set; }

    public DateTime? LastSyncStartedAt { get; set; }

    public DateTime? LastSyncCompletedAt { get; set; }

    public FeedSyncStatus LastSyncStatus { get; set; }

    public string? LastSyncError { get; set; }

    public int ConsecutiveFailureCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FeedSyncLog> SyncLogs { get; set; } = [];

    public ICollection<IngestedRecord> IngestedRecords { get; set; } = [];
}
