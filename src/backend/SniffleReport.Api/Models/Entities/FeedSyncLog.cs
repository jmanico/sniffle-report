using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.Entities;

public sealed class FeedSyncLog : EntityBase
{
    public Guid FeedSourceId { get; set; }

    public FeedSource FeedSource { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public FeedSyncStatus Status { get; set; }

    public int RecordsFetched { get; set; }

    public int RecordsCreated { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkippedDuplicate { get; set; }

    public int RecordsSkippedUnmappable { get; set; }

    public int AlertsPromoted { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorStackTrace { get; set; }
}
